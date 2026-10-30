using System.Collections.Generic;
using RimWorld;
using Verse;

namespace GodCursor
{
    [DefOf]
    public static class GC_DefOf
    {
        public static ThoughtDef GC_Grabbed;
        public static ThoughtDef GC_Thrown;
        public static ThoughtDef GC_Slammed;
        public static ThoughtDef GC_Placed;
        public static ThoughtDef GC_Poked;
        public static ThoughtDef GC_Patted;
        public static ThoughtDef GC_Worried;
        public static KeyBindingDef GC_ToggleGodCursor;

        static GC_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(GC_DefOf));
        }
    }

    /// <summary>What one pawn "thinks" about the cursor. Saved with the game.</summary>
    public class PawnCursorData : IExposable
    {
        public float attitude;   // -100 (hates/fears) .. +100 (worships)
        public int awareness;    // grows with every interaction; drives "fourth wall" thoughts
        public int grabs, throws, pokes, pats, hits;

        public void ExposeData()
        {
            Scribe_Values.Look(ref attitude, "attitude", 0f);
            Scribe_Values.Look(ref awareness, "awareness", 0);
            Scribe_Values.Look(ref grabs, "grabs", 0);
            Scribe_Values.Look(ref throws, "throws", 0);
            Scribe_Values.Look(ref pokes, "pokes", 0);
            Scribe_Values.Look(ref pats, "pats", 0);
            Scribe_Values.Look(ref hits, "hits", 0);
        }

        /// <summary>0 worship, 1 fond, 2 wary, 3 terror, -1 indifferent.</summary>
        public int AttitudeStage()
        {
            if (attitude >= 45f) return 0;
            if (attitude >= 15f) return 1;
            if (attitude <= -45f) return 3;
            if (attitude <= -15f) return 2;
            return -1;
        }

        /// <summary>-1 unaware, 0..3 growing awareness of the "other side".</summary>
        public int AwarenessStage()
        {
            if (awareness >= 25) return 3;
            if (awareness >= 15) return 2;
            if (awareness >= 8) return 1;
            if (awareness >= 3) return 0;
            return -1;
        }
    }

    public class GodCursorGameComp : GameComponent
    {
        public static GodCursorGameComp Instance;

        private Dictionary<int, PawnCursorData> data = new Dictionary<int, PawnCursorData>();

        public GodCursorGameComp(Game game)
        {
            Instance = this;
            Ctrl.Reset();
        }

        public PawnCursorData Get(Pawn p)
        {
            if (p == null) return null;
            PawnCursorData d;
            if (!data.TryGetValue(p.thingIDNumber, out d))
            {
                d = new PawnCursorData();
                data[p.thingIDNumber] = d;
            }
            return d;
        }

        public PawnCursorData Peek(Pawn p)
        {
            if (p == null) return null;
            PawnCursorData d;
            data.TryGetValue(p.thingIDNumber, out d);
            return d;
        }

        public override void GameComponentUpdate()
        {
            Ctrl.Update();
        }

        public override void GameComponentOnGUI()
        {
            Ctrl.OnGUI();
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref data, "pawnData", LookMode.Value, LookMode.Deep);
            if (data == null) data = new Dictionary<int, PawnCursorData>();
        }
    }

    public class ThoughtWorker_CursorAttitude : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            GodCursorSettings s = GodCursorMod.Settings;
            if (s == null || !s.fourthWall) return ThoughtState.Inactive;
            GodCursorGameComp comp = GodCursorGameComp.Instance;
            if (comp == null) return ThoughtState.Inactive;
            PawnCursorData d = comp.Peek(p);
            if (d == null) return ThoughtState.Inactive;
            int stage = d.AttitudeStage();
            return stage < 0 ? ThoughtState.Inactive : ThoughtState.ActiveAtStage(stage);
        }
    }

    public class ThoughtWorker_FourthWall : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            GodCursorSettings s = GodCursorMod.Settings;
            if (s == null || !s.fourthWall) return ThoughtState.Inactive;
            GodCursorGameComp comp = GodCursorGameComp.Instance;
            if (comp == null) return ThoughtState.Inactive;
            PawnCursorData d = comp.Peek(p);
            if (d == null) return ThoughtState.Inactive;
            int stage = d.AwarenessStage();
            return stage < 0 ? ThoughtState.Inactive : ThoughtState.ActiveAtStage(stage);
        }
    }
}
