using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace GodCursor
{
    public enum Act { Grab, Place, Throw, Landed, Hit, Pat, Poke }

    /// <summary>A pawn that is in the air (thrown, dropped, or doing a small hop).</summary>
    public class Flight
    {
        public Pawn pawn;
        public Vector2 pos;      // ground position in map coordinates (x, z)
        public Vector2 vel;      // cells per second
        public float h;          // height above the ground, in cells
        public float vh;         // vertical speed
        public float zOff;       // visual offset so the cursor is at the pawn's feet
        public int bounces;
        public int hits;
        public float wallCooldown;
        public bool thrown;
        public bool hop;
    }

    class Bubble
    {
        public Pawn pawn;
        public string text;
        public float start;
        public float until;
    }

    public static class Ctrl
    {
        // ---- tuning ----
        const float Gravity = 22f;
        const float LiftHeight = 0.55f;
        const float ThrowSpeedThreshold = 9f;   // cells/sec of cursor speed on release that counts as a throw
        const float MaxThrowSpeed = 80f;
        const float DrawAltitude = 1.25f;       // above pawns/buildings, below UI overlays

        // ---- state ----
        public static bool Active;

        static Pawn held;
        static bool dragging;
        static float grabTime;
        static Vector2 grabStart;
        static Vector2 heldVisual;
        static float lift;
        static float heldTremble;

        static readonly List<KeyValuePair<float, Vector2>> samples = new List<KeyValuePair<float, Vector2>>();
        static readonly Dictionary<Pawn, Flight> flights = new Dictionary<Pawn, Flight>();
        static readonly List<Flight> tmpFlights = new List<Flight>();
        static readonly Dictionary<Pawn, float> nervous = new Dictionary<Pawn, float>();
        static readonly List<Pawn> tmpPawns = new List<Pawn>();
        static readonly Dictionary<Pawn, float> lastWorry = new Dictionary<Pawn, float>();
        static readonly List<Bubble> bubbles = new List<Bubble>();
        static readonly Dictionary<Pawn, int> panicPawns = new Dictionary<Pawn, int>();   // pawns WE made panic -> tick it started
        static readonly List<Pawn> tmpPanic = new List<Pawn>();
        static float nextChatter;

        static Pawn hoverPawn;
        static float hoverTime;
        static bool hoverFaced;
        static bool hoverReacted;

        public static GodCursorSettings S { get { return GodCursorMod.Settings; } }

        public static bool AnyOverride
        {
            get { return dragging || flights.Count > 0 || nervous.Count > 0; }
        }

        public static void Reset()
        {
            Active = false;
            held = null;
            dragging = false;
            hoverPawn = null;
            samples.Clear();
            flights.Clear();
            nervous.Clear();
            lastWorry.Clear();
            bubbles.Clear();
            panicPawns.Clear();
            nextChatter = Time.unscaledTime + 25f;
        }

        public static void SetActive(bool value)
        {
            if (Active == value) return;
            Active = value;
            if (!Active)
            {
                if (held != null) DropHeld();
                hoverPawn = null;
            }
            Messages.Message((value ? "GC_MsgOn" : "GC_MsgOff").Translate(), MessageTypeDefOf.SilentInput, false);
        }

        public static bool IsLocked(Pawn p)
        {
            if (held == null && flights.Count == 0) return false;
            if (dragging && p == held) return true;
            return flights.ContainsKey(p);
        }

        // =====================================================================
        //  Input
        // =====================================================================

        /// <summary>
        /// Called from a Harmony prefix on Selector.HandleMapClicks, which runs only for input
        /// that no UI element has used yet. Returns true when the click was consumed.
        /// </summary>
        public static bool HandleSelectorInput()
        {
            if (!Active) return false;
            if (held != null) return true;   // swallow everything while a pawn is held (no box-select)

            Event e = Event.current;
            if (e == null || e.type != EventType.MouseDown || e.button != 0) return false;

            Map map = Find.CurrentMap;
            if (map == null) return false;
            Pawn p = PawnAt(map, UI.MouseMapPosition());
            if (p == null) return false;

            e.Use();
            if (e.shift) Poke(p);
            else if (e.control) Pat(p);
            else BeginHold(p, UI.MouseMapPosition());
            return true;
        }

        static void HandleHotkey()
        {
            KeyBindingDef key = GC_DefOf.GC_ToggleGodCursor;
            if (key != null && key.KeyDownEvent)
            {
                Toggle();
                Event.current.Use();
            }
        }

        public static void Toggle()
        {
            SetActive(!Active);
            if (Active && !Find.WindowStack.IsOpen<Window_GodCursor>())
                Find.WindowStack.Add(new Window_GodCursor());
        }

        static bool MouseOverUI()
        {
            Vector2 mp = UI.MousePositionOnUIInverted;
            if (Find.WindowStack.GetWindowAt(mp) != null) return true;
            if (mp.y > UI.screenHeight - 36f) return true;
            return false;
        }

        // =====================================================================
        //  Picking up, dragging, releasing
        // =====================================================================

        static Pawn PawnAt(Map map, Vector3 m)
        {
            Pawn best = null;
            float bestD = 999f;
            IReadOnlyList<Pawn> list = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < list.Count; i++)
            {
                Pawn p = list[i];
                if (p == null || p.Dead || !p.Spawned) continue;
                if (flights.ContainsKey(p)) continue;
                Vector3 dp = p.DrawPos;
                float r = Mathf.Clamp(0.42f + 0.18f * p.BodySize, 0.5f, 1.3f);
                float dx = dp.x - m.x;
                float dz = (dp.z + 0.2f) - m.z;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d < r && d < bestD)
                {
                    best = p;
                    bestD = d;
                }
            }
            return best;
        }

        static void BeginHold(Pawn p, Vector3 m)
        {
            held = p;
            dragging = false;
            grabTime = Time.unscaledTime;
            grabStart = new Vector2(m.x, m.z);
            heldVisual = grabStart;
            lift = 0f;
            samples.Clear();
            PushSample(grabStart);
        }

        static void StartDrag()
        {
            dragging = true;
            if (held.jobs != null) held.jobs.StopAll(false, true);
            if (held.pather != null) held.pather.StopDead();
            Interact(held, Act.Grab);
            CalmPawn(held, true);   // "caught" - a fleeing pawn is calmed the moment you hold it
        }

        static void PushSample(Vector2 v)
        {
            samples.Add(new KeyValuePair<float, Vector2>(Time.unscaledTime, v));
            if (samples.Count > 24) samples.RemoveAt(0);
        }

        static Vector2 Velocity()
        {
            if (samples.Count < 2) return Vector2.zero;
            KeyValuePair<float, Vector2> last = samples[samples.Count - 1];
            KeyValuePair<float, Vector2> first = last;
            for (int i = samples.Count - 2; i >= 0; i--)
            {
                if (last.Key - samples[i].Key > 0.12f) break;
                first = samples[i];
            }
            float dt = last.Key - first.Key;
            if (dt < 0.02f) return Vector2.zero;
            return (last.Value - first.Value) / dt;
        }

        static Vector2 ClampToMap(Map map, Vector2 v)
        {
            v.x = Mathf.Clamp(v.x, 0.5f, map.Size.x - 0.5f);
            v.y = Mathf.Clamp(v.y, 0.5f, map.Size.z - 0.5f);
            return v;
        }

        static IntVec3 CellOf(Vector2 v)
        {
            return new IntVec3(Mathf.FloorToInt(v.x), 0, Mathf.FloorToInt(v.y));
        }

        static bool Blocked(IntVec3 c, Map map)
        {
            return !c.InBounds(map) || c.Impassable(map);
        }

        static void UpdateHeld(Map map, float dt)
        {
            if (!Active || held.Destroyed || held.Dead || !held.Spawned || held.Map != map)
            {
                DropHeld();
                return;
            }

            Vector3 m = UI.MouseMapPosition();
            Vector2 mp = ClampToMap(map, new Vector2(m.x, m.z));
            PushSample(mp);
            bool down = Input.GetMouseButton(0);

            if (!dragging)
            {
                if (!down)
                {
                    Release(map);
                    return;
                }
                if ((mp - grabStart).magnitude > 0.4f || Time.unscaledTime - grabTime > 0.22f)
                    StartDrag();
                else
                    return;
            }

            if (!down)
            {
                Release(map);
                return;
            }

            lift = Mathf.MoveTowards(lift, LiftHeight, dt * 6f);
            heldVisual = Vector2.Lerp(heldVisual, mp, 1f - Mathf.Exp(-28f * dt));

            GodCursorGameComp comp = GodCursorGameComp.Instance;
            PawnCursorData d = comp != null ? comp.Peek(held) : null;
            heldTremble = (S.fourthWall && d != null && d.attitude <= -15f) ? 0.045f : 0f;

            IntVec3 c = CellOf(mp);
            if (c != held.Position && !Blocked(c, map)) held.Position = c;

            Vector2 v = Velocity();
            if (Mathf.Abs(v.x) > 2f) held.Rotation = v.x > 0f ? Rot4.East : Rot4.West;
            else held.Rotation = Rot4.South;
        }

        static void Release(Map map)
        {
            Pawn p = held;
            bool wasDragging = dragging;
            held = null;
            dragging = false;

            if (!wasDragging)
            {
                // a plain click: behave like the vanilla selector would
                Find.Selector.ClearSelection();
                Find.Selector.Select(p, true, true);
                return;
            }

            Vector2 v = Velocity();
            bool thrown = v.magnitude > ThrowSpeedThreshold;
            if (thrown)
            {
                v *= S.throwPower;
                if (v.magnitude > MaxThrowSpeed) v = v.normalized * MaxThrowSpeed;
            }
            else
            {
                v = Vector2.zero;
            }

            Flight f = new Flight();
            f.pawn = p;
            f.pos = heldVisual;
            f.vel = v;
            f.h = lift;
            f.vh = thrown ? Mathf.Clamp(3.5f + v.magnitude * 0.11f, 3.5f, 11f) : 0f;
            f.zOff = 0.15f;
            f.thrown = thrown;
            flights[p] = f;

            if (thrown) Interact(p, Act.Throw);
        }

        static void DropHeld()
        {
            if (held == null) return;
            Pawn p = held;
            bool wasDragging = dragging;
            held = null;
            dragging = false;
            if (!wasDragging || p.Destroyed || p.Dead || !p.Spawned) return;

            Flight f = new Flight();
            f.pawn = p;
            f.pos = heldVisual;
            f.h = lift;
            f.zOff = 0.15f;
            flights[p] = f;
        }

        static void StartHop(Pawn p, float vh)
        {
            if (flights.ContainsKey(p) || (held == p)) return;
            Vector3 dp = p.DrawPos;   // read before the pawn is registered as a flight
            Flight f = new Flight();
            f.pawn = p;
            f.pos = new Vector2(dp.x, dp.z);
            f.vh = vh;
            f.hop = true;
            flights[p] = f;
        }

        // =====================================================================
        //  Flight simulation
        // =====================================================================

        static void UpdateFlights(float dt)
        {
            if (flights.Count == 0) return;
            tmpFlights.Clear();
            foreach (KeyValuePair<Pawn, Flight> kv in flights) tmpFlights.Add(kv.Value);
            for (int i = 0; i < tmpFlights.Count; i++) UpdateFlight(tmpFlights[i], dt);
            tmpFlights.Clear();
        }

        static void UpdateFlight(Flight f, float dt)
        {
            Pawn p = f.pawn;
            if (p == null) return;
            if (p.Destroyed || p.Dead || !p.Spawned)
            {
                flights.Remove(p);
                return;
            }
            Map map = p.Map;
            Vector2 prev = f.pos;

            f.pos += f.vel * dt;
            f.h += f.vh * dt;
            f.vh -= Gravity * dt;
            if (f.wallCooldown > 0f) f.wallCooldown -= dt;

            if (!f.hop)
            {
                Vector2 before = f.vel;
                bool hitWall = false;

                if (S.forbidOffMap)
                {
                    if (f.pos.x < 0.5f || f.pos.x > map.Size.x - 0.5f)
                    {
                        f.pos.x = Mathf.Clamp(f.pos.x, 0.5f, map.Size.x - 0.5f);
                        f.vel.x = -f.vel.x * 0.5f;
                        hitWall = true;
                    }
                    if (f.pos.y < 0.5f || f.pos.y > map.Size.z - 0.5f)
                    {
                        f.pos.y = Mathf.Clamp(f.pos.y, 0.5f, map.Size.z - 0.5f);
                        f.vel.y = -f.vel.y * 0.5f;
                        hitWall = true;
                    }
                }
                else if (f.pos.x < -0.3f || f.pos.x > map.Size.x + 0.3f || f.pos.y < -0.3f || f.pos.y > map.Size.z + 0.3f)
                {
                    LaunchOffMap(f);
                    return;
                }

                IntVec3 c = CellOf(f.pos);
                if (f.h < 1.0f && c.InBounds(map) && c.Impassable(map))
                {
                    bool bx = Blocked(CellOf(new Vector2(f.pos.x, prev.y)), map);
                    bool bz = Blocked(CellOf(new Vector2(prev.x, f.pos.y)), map);
                    bool flipX = bx || !bz;
                    bool flipZ = bz || !bx;
                    if (flipX) { f.vel.x = -f.vel.x * 0.5f; f.pos.x = prev.x; }
                    if (flipZ) { f.vel.y = -f.vel.y * 0.5f; f.pos.y = prev.y; }
                    hitWall = true;
                }

                if (hitWall && f.wallCooldown <= 0f)
                {
                    f.wallCooldown = 0.15f;
                    Impact(f, before.magnitude);
                    if (p.Destroyed || p.Dead || !p.Spawned)
                    {
                        flights.Remove(p);
                        return;
                    }
                }

                // keep the real position in sync (so the pawn is drawn/culled correctly), but never inside walls
                IntVec3 cell = CellOf(f.pos);
                if (cell != p.Position && !Blocked(cell, map)) p.Position = cell;

                // tumble while airborne
                int spin = (int)(Time.unscaledTime * 10f) & 3;
                p.Rotation = spin == 0 ? Rot4.North : spin == 1 ? Rot4.East : spin == 2 ? Rot4.South : Rot4.West;
            }

            if (f.h <= 0f && f.vh < 0f)
            {
                f.h = 0f;
                float horiz = f.vel.magnitude;
                float impact = Mathf.Sqrt(f.vh * f.vh + horiz * horiz);

                if (!f.hop)
                {
                    Impact(f, impact);
                    if (p.Destroyed || p.Dead || !p.Spawned)
                    {
                        flights.Remove(p);
                        return;
                    }
                    if (impact > 9f && f.bounces < 2)
                    {
                        f.bounces++;
                        f.vh = -f.vh * 0.45f;
                        f.vel *= 0.6f;
                        return;
                    }
                }
                Land(f);
            }
        }

        /// <summary>Only reachable when "keep pawns on the map" is OFF: the pawn leaves the map for real.</summary>
        static void LaunchOffMap(Flight f)
        {
            Pawn p = f.pawn;
            flights.Remove(p);
            panicPawns.Remove(p);
            string label = p.LabelShort;
            bool ours = p.Faction == Faction.OfPlayer;
            try
            {
                p.ExitMap(false, Rot4.Invalid);
            }
            catch (System.Exception e)
            {
                Log.Error("[GodCursor] ExitMap failed, keeping the pawn on the map: " + e);
                if (p.Spawned) p.Notify_Teleported(true, true);
                return;
            }
            Messages.Message("GC_MsgYeeted".Translate(label), ours ? MessageTypeDefOf.NegativeEvent : MessageTypeDefOf.NeutralEvent, false);
        }

        static void Impact(Flight f, float speed)
        {
            Pawn p = f.pawn;
            if (f.hop || speed < 4f) return;
            FleckMaker.ThrowDustPuff(p.Position.ToVector3Shifted(), p.Map, Mathf.Clamp(speed / 8f, 0.6f, 2f));
            if (speed < 6.5f || f.hits >= 3) return;
            f.hits++;
            if (!S.damage) return;

            float amount = (speed - 5f) * 0.9f * S.damageMult;
            float cap = S.lethal ? 40f : 10f;
            amount = Mathf.Clamp(amount, 1f, cap);
            if (!S.lethal && p.health != null && p.health.summaryHealth.SummaryHealthPercent < 0.4f) return;

            p.TakeDamage(new DamageInfo(DamageDefOf.Blunt, amount, 0f, -1f));
            if (!p.Dead && p.Spawned) Interact(p, Act.Hit);
        }

        static void Land(Flight f)
        {
            Pawn p = f.pawn;
            flights.Remove(p);
            if (p.Destroyed || p.Dead || !p.Spawned) return;

            if (f.hop) return;

            Map map = p.Map;
            IntVec3 cell = FindLanding(p, map, f.pos);
            if (cell != p.Position) p.Position = cell;
            p.Notify_Teleported(true, true);
            p.Rotation = Rot4.South;

            if (!f.thrown) Interact(p, Act.Place);
            else if (f.hits == 0 || !S.damage) Interact(p, Act.Landed);
        }

        static IntVec3 FindLanding(Pawn p, Map map, Vector2 pos)
        {
            IntVec3 c = CellOf(pos);
            if (c.InBounds(map) && c.Standable(map) && c.Walkable(map)) return c;
            foreach (IntVec3 n in GenRadial.RadialCellsAround(c, 14f, true))
            {
                if (n.InBounds(map) && n.Standable(map) && n.Walkable(map)) return n;
            }
            return p.Position;
        }

        // =====================================================================
        //  Reactions: thoughts, attitude, bubbles, panic
        // =====================================================================

        static void Poke(Pawn p)
        {
            Interact(p, Act.Poke);
            MakeNervous(p, 1.2f);
            StartHop(p, 4.5f);
        }

        static void Pat(Pawn p)
        {
            CalmPawn(p, false);
            Interact(p, Act.Pat);
            StartHop(p, 2.6f);
        }

        static void Gain(Pawn p, ThoughtDef def)
        {
            if (def == null || p.needs == null || p.needs.mood == null || p.needs.mood.thoughts == null) return;
            p.needs.mood.thoughts.memories.TryGainMemory(def, null, null);
        }

        static void Interact(Pawn p, Act act)
        {
            GodCursorSettings s = S;
            GodCursorGameComp comp = GodCursorGameComp.Instance;
            PawnCursorData d = comp != null ? comp.Get(p) : null;
            int before = d != null ? d.AwarenessStage() : -1;

            ThoughtDef thought = null;
            string pool = null;
            float att = 0f;
            int aw = 0;
            float panicChance = 0f;

            switch (act)
            {
                case Act.Grab:
                    thought = GC_DefOf.GC_Grabbed; pool = "GC_B_Grab"; att = -0.5f; aw = 1;
                    if (d != null) d.grabs++;
                    break;
                case Act.Throw:
                    thought = GC_DefOf.GC_Thrown; pool = "GC_B_Throw"; att = -5f; aw = 2; panicChance = 0.12f;
                    if (d != null) d.throws++;
                    break;
                case Act.Landed:
                    pool = "GC_B_Landed";
                    break;
                case Act.Place:
                    thought = GC_DefOf.GC_Placed; pool = "GC_B_Land"; att = 3f;
                    break;
                case Act.Hit:
                    thought = GC_DefOf.GC_Slammed; pool = "GC_B_Hit"; att = -10f; panicChance = 0.3f;
                    if (d != null) d.hits++;
                    break;
                case Act.Pat:
                    thought = GC_DefOf.GC_Patted; pool = "GC_B_Pat"; att = 8f; aw = 1;
                    if (d != null) d.pats++;
                    break;
                case Act.Poke:
                    thought = GC_DefOf.GC_Poked; pool = "GC_B_Poke"; att = -6f; aw = 1; panicChance = 0.08f;
                    if (d != null) d.pokes++;
                    break;
            }

            if (d != null)
            {
                d.attitude = Mathf.Clamp(d.attitude + att, -100f, 100f);
                d.awareness += aw;
            }
            Gain(p, thought);

            if (!s.fourthWall) return;

            int after = d != null ? d.AwarenessStage() : -1;
            string text;
            if (!p.RaceProps.Humanlike) text = Texts.Pick("GC_B_Animal");
            else if (after >= 0 && Rand.Chance(0.35f)) text = Texts.Pick("GC_B_Meta" + after);
            else text = Texts.Pick(pool);
            Say(p, text);

            if (after > before && after >= 1 && p.RaceProps.Humanlike && p.Faction == Faction.OfPlayer)
            {
                Messages.Message(("GC_Milestone_" + after).Translate(p.LabelShort), new LookTargets(p), MessageTypeDefOf.NeutralEvent, false);
            }

            if (panicChance > 0f)
            {
                if (d != null && d.attitude <= -45f) panicChance += 0.1f;
                MaybePanic(p, panicChance);
            }
        }

        static void MaybePanic(Pawn p, float chance)
        {
            if (!S.panic || !S.fourthWall) return;
            if (p.Dead || p.Downed || !p.Spawned || p.InMentalState) return;
            if (p.mindState == null || p.mindState.mentalStateHandler == null) return;
            if (!Rand.Chance(chance)) return;
            p.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee, "GC_PanicReason".Translate().ToString(), true);
            if (IsFearState(p))
            {
                panicPawns[p] = Find.TickManager.TicksGame;
                Say(p, Texts.Pick(p.RaceProps.Humanlike ? "GC_B_PanicStart" : "GC_B_Animal"));
            }
        }

        /// <summary>Panic / terror, i.e. the states a pat or a grab is allowed to cancel.</summary>
        public static bool IsFearState(Pawn p)
        {
            if (p == null || !p.InMentalState) return false;
            MentalStateDef def = p.MentalStateDef;
            return def == MentalStateDefOf.PanicFlee || def == MentalStateDefOf.PanicFleeFire || def == MentalStateDefOf.Terror;
        }

        public static bool CalmPawn(Pawn p, bool speak)
        {
            if (p == null) return false;
            panicPawns.Remove(p);
            if (!IsFearState(p)) return false;
            p.MentalState.RecoverFromState();
            if (p.pather != null) p.pather.StopDead();
            if (speak && S.fourthWall) Say(p, Texts.Pick(p.RaceProps.Humanlike ? "GC_B_Calm" : "GC_B_Animal"));
            return true;
        }

        /// <summary>Panel button: calm every frightened pawn on the current map.</summary>
        public static int CalmAll()
        {
            Map map = Find.CurrentMap;
            if (map == null) return 0;
            tmpPawns.Clear();
            IReadOnlyList<Pawn> list = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < list.Count; i++)
                if (IsFearState(list[i])) tmpPawns.Add(list[i]);
            int n = 0;
            for (int i = 0; i < tmpPawns.Count; i++)
                if (CalmPawn(tmpPawns[i], true)) n++;
            tmpPawns.Clear();
            Messages.Message("GC_MsgCalmed".Translate(n.ToString()), MessageTypeDefOf.SilentInput, false);
            return n;
        }

        /// <summary>Calms cursor-induced panic after a timeout, and always before the pawn can reach the map edge.</summary>
        static void UpdatePanic()
        {
            if (panicPawns.Count == 0) return;
            int now = Find.TickManager.TicksGame;
            int limit = Mathf.RoundToInt(S.panicDuration * 60f);
            tmpPanic.Clear();
            foreach (KeyValuePair<Pawn, int> kv in panicPawns) tmpPanic.Add(kv.Key);
            for (int i = 0; i < tmpPanic.Count; i++)
            {
                Pawn p = tmpPanic[i];
                int since = panicPawns[p];
                if (p.Destroyed || !p.Spawned || !IsFearState(p))
                {
                    panicPawns.Remove(p);
                    continue;
                }
                if (S.forbidOffMap && !p.Position.InBounds(p.Map, 3))
                {
                    CalmPawn(p, false);
                    if (S.fourthWall) Say(p, Texts.Pick(p.RaceProps.Humanlike ? "GC_B_Edge" : "GC_B_Animal"));
                }
                else if (now - since > limit)
                {
                    CalmPawn(p, true);
                }
            }
            tmpPanic.Clear();
        }

        static void MakeNervous(Pawn p, float seconds)
        {
            nervous[p] = Time.unscaledTime + seconds;
        }

        static void CleanupNervous()
        {
            if (nervous.Count == 0) return;
            float now = Time.unscaledTime;
            tmpPawns.Clear();
            foreach (KeyValuePair<Pawn, float> kv in nervous)
                if (kv.Value < now || kv.Key == null || !kv.Key.Spawned) tmpPawns.Add(kv.Key);
            for (int i = 0; i < tmpPawns.Count; i++) nervous.Remove(tmpPawns[i]);
            tmpPawns.Clear();
        }

        static void Say(Pawn p, string text)
        {
            if (p == null || string.IsNullOrEmpty(text)) return;
            bubbles.RemoveAll(b => b.pawn == p);
            if (bubbles.Count > 24) bubbles.RemoveAt(0);
            Bubble b2 = new Bubble();
            b2.pawn = p;
            b2.text = text;
            b2.start = Time.unscaledTime;
            b2.until = b2.start + 2.8f;
            bubbles.Add(b2);
        }

        // =====================================================================
        //  Hovering: the "fourth wall" part
        // =====================================================================

        static void UpdateHover(Map map, float dt)
        {
            if (!Active || held != null || !S.fourthWall || MouseOverUI())
            {
                hoverPawn = null;
                hoverTime = 0f;
                return;
            }

            Pawn p = PawnAt(map, UI.MouseMapPosition());
            if (p != hoverPawn)
            {
                hoverPawn = p;
                hoverTime = 0f;
                hoverFaced = false;
                hoverReacted = false;
                return;
            }
            if (p == null) return;

            hoverTime += dt;

            if (!hoverFaced && hoverTime > 0.6f)
            {
                hoverFaced = true;
                if (p.rotationTracker != null) p.rotationTracker.FaceCell(UI.MouseCell());
            }

            if (!hoverReacted && hoverTime > 1.2f)
            {
                hoverReacted = true;
                HoverReact(p);
            }
            else if (hoverReacted && hoverTime > 9f)
            {
                hoverTime = 0f;
                hoverReacted = false;
            }
        }

        static void HoverReact(Pawn p)
        {
            GodCursorGameComp comp = GodCursorGameComp.Instance;
            PawnCursorData d = comp != null ? comp.Peek(p) : null;
            int st = d != null ? d.AttitudeStage() : -1;
            int aware = d != null ? d.AwarenessStage() : -1;

            string text;
            if (!p.RaceProps.Humanlike)
            {
                text = Texts.Pick("GC_B_Animal");
            }
            else if (aware >= 1 && Rand.Chance(0.4f))
            {
                text = Texts.Pick("GC_B_Meta" + aware);
            }
            else if (st == 0) text = Texts.Pick("GC_B_Hover_Worship");
            else if (st == 1) text = Texts.Pick("GC_B_Hover_Friendly");
            else if (st == 2) text = Texts.Pick("GC_B_Hover_Wary");
            else if (st == 3) text = Texts.Pick("GC_B_Hover_Terror");
            else text = Texts.Pick("GC_B_Hover_Neutral");
            Say(p, text);

            // everybody except admirers gets nervous when the cursor lingers over them
            if (st == 0 || st == 1) return;

            MakeNervous(p, 2.5f);
            float last;
            float now = Time.unscaledTime;
            if (!lastWorry.TryGetValue(p, out last) || now - last > 20f)
            {
                lastWorry[p] = now;
                Gain(p, GC_DefOf.GC_Worried);
            }
            if (st == 3) MaybePanic(p, 0.08f);
        }

        /// <summary>Pawns that noticed the cursor occasionally talk to themselves or to the player.</summary>
        static void UpdateChatter(Map map)
        {
            float now = Time.unscaledTime;
            if (now < nextChatter) return;
            nextChatter = now + Rand.Range(18f, 40f);
            if (!S.ambientChatter || !S.fourthWall) return;

            GodCursorGameComp comp = GodCursorGameComp.Instance;
            if (comp == null) return;

            tmpPawns.Clear();
            IReadOnlyList<Pawn> list = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < list.Count; i++)
            {
                Pawn p = list[i];
                if (p.Dead || p.Downed || !p.RaceProps.Humanlike || IsLocked(p)) continue;
                PawnCursorData d = comp.Peek(p);
                if (d == null || d.AwarenessStage() < 0) continue;
                tmpPawns.Add(p);
            }
            if (tmpPawns.Count > 0)
            {
                Pawn pick = tmpPawns[Rand.Range(0, tmpPawns.Count)];
                PawnCursorData pd = comp.Peek(pick);
                Say(pick, Texts.Pick("GC_B_Idle" + pd.AwarenessStage()));
            }
            tmpPawns.Clear();
        }

        // =====================================================================
        //  Per-frame update / drawing
        // =====================================================================

        public static void Update()
        {
            if (Current.ProgramState != ProgramState.Playing) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);

            UpdateFlights(dt);
            CleanupNervous();
            UpdatePanic();

            Map map = Find.CurrentMap;
            if (map == null || WorldRendererUtility.WorldRendered)
            {
                if (held != null) DropHeld();
                hoverPawn = null;
                return;
            }

            if (held != null) UpdateHeld(map, dt);
            UpdateHover(map, dt);
            UpdateChatter(map);
            DrawMarkers();
        }

        static Vector3 Ground(Vector2 v)
        {
            return new Vector3(v.x, 0f, v.y);
        }

        static void DrawMarkers()
        {
            if (dragging && held != null) GenDraw.DrawCircleOutline(Ground(heldVisual), 0.5f);
            foreach (KeyValuePair<Pawn, Flight> kv in flights)
                if (!kv.Value.hop) GenDraw.DrawCircleOutline(Ground(kv.Value.pos), 0.45f);
            if (Active && held == null && hoverPawn != null && hoverPawn.Spawned) GenDraw.DrawTargetHighlight(hoverPawn);
        }

        /// <summary>Called from Harmony postfixes on the pawn's DrawPos getters.</summary>
        public static void ModifyDrawPos(Pawn p, ref Vector3 pos)
        {
            if (p == null) return;

            if (dragging && p == held)
            {
                float sway = Mathf.Sin(Time.unscaledTime * 9f) * 0.03f + Mathf.Sin(Time.unscaledTime * 70f) * heldTremble;
                pos = new Vector3(heldVisual.x + sway, DrawAltitude, heldVisual.y + 0.15f + lift);
                return;
            }

            Flight f;
            if (flights.TryGetValue(p, out f))
            {
                pos = new Vector3(f.pos.x, DrawAltitude, f.pos.y + f.zOff + f.h);
                return;
            }

            float until;
            if (nervous.TryGetValue(p, out until))
            {
                pos.x += Mathf.Sin(Time.unscaledTime * 70f) * 0.035f;
            }
        }

        public static void OnGUI()
        {
            HandleHotkey();

            if (Current.ProgramState != ProgramState.Playing) return;
            if (Event.current.type != EventType.Repaint) return;
            if (Find.CurrentMap == null || WorldRendererUtility.WorldRendered) return;

            DrawBubbles();
            DrawMouseLabel();
        }

        static void DrawBubbles()
        {
            if (bubbles.Count == 0) return;
            float now = Time.unscaledTime;
            bubbles.RemoveAll(b => b.until < now || b.pawn == null || !b.pawn.Spawned);

            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;

            for (int i = 0; i < bubbles.Count; i++)
            {
                Bubble b = bubbles[i];
                if (b.pawn.Map != Find.CurrentMap) continue;

                float alpha = Mathf.Clamp01((b.until - now) / 0.5f);
                Vector2 pos = GenMapUI.LabelDrawPosFor(b.pawn, 0.8f);
                float w = Mathf.Min(Text.CalcSize(b.text).x + 16f, 210f);
                float h = Text.CalcHeight(b.text, w - 8f) + 6f;
                Rect r = new Rect(pos.x - w / 2f, pos.y - h - 4f, w, h);

                Widgets.DrawBoxSolidWithOutline(r, new Color(1f, 1f, 1f, 0.93f * alpha), new Color(0.15f, 0.15f, 0.15f, alpha), 1);
                GUI.color = new Color(0.08f, 0.08f, 0.08f, alpha);
                Widgets.Label(r, b.text);
                GUI.color = Color.white;
            }

            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
        }

        static void DrawMouseLabel()
        {
            if (!Active || MouseOverUI()) return;

            string text = null;
            if (dragging)
            {
                if (S.hintHold) text = "GC_MouseHold".Translate().ToString();
            }
            else if (hoverPawn != null && hoverPawn.Spawned)
            {
                if (S.hintHover)
                {
                    GodCursorGameComp comp = GodCursorGameComp.Instance;
                    PawnCursorData d = comp != null ? comp.Peek(hoverPawn) : null;
                    text = "GC_MouseHover".Translate(hoverPawn.LabelShort, AttitudeLabel(d)).ToString();
                }
            }
            else if (S.hintIdle)
            {
                text = "GC_MouseIdle".Translate().ToString();
            }
            if (text == null) return;

            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;

            Vector2 mp = UI.MousePositionOnUIInverted;
            Vector2 size = Text.CalcSize(text);
            Rect r = new Rect(mp.x + 18f, mp.y + 18f, size.x + 10f, size.y + 4f);
            if (r.xMax > UI.screenWidth) r.x = mp.x - r.width - 8f;
            if (r.yMax > UI.screenHeight) r.y = mp.y - r.height - 8f;

            Widgets.DrawBoxSolid(r, new Color(0f, 0f, 0f, 0.6f));
            Rect inner = new Rect(r.x + 5f, r.y + 2f, size.x + 2f, size.y);
            Widgets.Label(inner, text);

            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
        }

        static string AttitudeLabel(PawnCursorData d)
        {
            int st = d != null ? d.AttitudeStage() : -1;
            string key = st == 0 ? "GC_Att_Worship" : st == 1 ? "GC_Att_Fond" : st == 2 ? "GC_Att_Wary" : st == 3 ? "GC_Att_Terror" : "GC_Att_Neutral";
            return key.Translate().ToString();
        }
    }

    public static class Texts
    {
        static readonly Dictionary<string, int> counts = new Dictionary<string, int>();

        /// <summary>
        /// Picks a random line from the pool PREFIX_1 .. PREFIX_N. N is discovered from the language files,
        /// so lines can be added to Keyed/*.xml without touching the code.
        /// </summary>
        public static string Pick(string prefix)
        {
            int n;
            if (!counts.TryGetValue(prefix, out n))
            {
                n = 0;
                while (Translator.CanTranslate(prefix + "_" + (n + 1))) n++;
                counts[prefix] = n;
            }
            if (n == 0) return "...";
            return (prefix + "_" + Rand.RangeInclusive(1, n)).Translate().ToString();
        }
    }
}
