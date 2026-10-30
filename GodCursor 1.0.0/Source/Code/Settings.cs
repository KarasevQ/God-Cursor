using UnityEngine;
using Verse;

namespace GodCursor
{
    public class GodCursorSettings : ModSettings
    {
        public bool damage = false;       // throws hurt pawns
        public bool lethal = false;       // damage may kill
        public bool fourthWall = true;    // bubbles, attitude, awareness
        public bool panic = true;         // scared pawns may panic
        public float throwPower = 1f;
        public float damageMult = 1f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref damage, "damage", false);
            Scribe_Values.Look(ref lethal, "lethal", false);
            Scribe_Values.Look(ref fourthWall, "fourthWall", true);
            Scribe_Values.Look(ref panic, "panic", true);
            Scribe_Values.Look(ref throwPower, "throwPower", 1f);
            Scribe_Values.Look(ref damageMult, "damageMult", 1f);
            base.ExposeData();
        }
    }

    public class GodCursorMod : Mod
    {
        public static GodCursorSettings Settings;

        public GodCursorMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<GodCursorSettings>();
        }

        public override string SettingsCategory()
        {
            return "GC_ModName".Translate().ToString();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard l = new Listing_Standard();
            l.Begin(inRect);
            DrawOptions(l);
            l.End();
        }

        /// <summary>Shared by the mod settings page and the in-game panel.</summary>
        public static void DrawOptions(Listing_Standard l)
        {
            GodCursorSettings s = Settings;
            l.CheckboxLabeled("GC_Damage".Translate().ToString(), ref s.damage, 0f);
            l.CheckboxLabeled("GC_Lethal".Translate().ToString(), ref s.lethal, 0f);
            l.CheckboxLabeled("GC_FourthWall".Translate().ToString(), ref s.fourthWall, 0f);
            l.CheckboxLabeled("GC_Panic".Translate().ToString(), ref s.panic, 0f);
            l.Gap(6f);
            l.Label("GC_ThrowPower".Translate(s.throwPower.ToString("F1")).ToString());
            s.throwPower = l.Slider(s.throwPower, 0.5f, 2.5f);
            l.Label("GC_DamageMult".Translate(s.damageMult.ToString("F1")).ToString());
            s.damageMult = l.Slider(s.damageMult, 0.25f, 3f);
        }
    }
}
