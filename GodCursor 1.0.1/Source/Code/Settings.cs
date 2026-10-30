using System;
using UnityEngine;
using Verse;

namespace GodCursor
{
    public class GodCursorSettings : ModSettings
    {
        public bool damage = false;          // throws hurt pawns
        public bool lethal = false;          // damage may kill
        public bool fourthWall = true;       // bubbles, attitude, awareness
        public bool panic = false;           // scared pawns may panic (off by default)
        public float panicDuration = 8f;     // seconds of game time before a cursor-induced panic is cancelled
        public bool forbidOffMap = true;     // thrown pawns bounce at the edge; panicking pawns are stopped near it
        public bool ambientChatter = true;   // aware pawns occasionally talk on their own
        public bool hintIdle = false;        // "God cursor" label next to the cursor
        public bool hintHover = true;        // pawn name + attitude when hovering
        public bool hintHold = false;        // "release to set down" hint while holding
        public bool hintPanel = true;        // controls help in the panel
        public float throwPower = 1f;
        public float damageMult = 1f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref damage, "damage", false);
            Scribe_Values.Look(ref lethal, "lethal", false);
            Scribe_Values.Look(ref fourthWall, "fourthWall", true);
            Scribe_Values.Look(ref panic, "panic", false);
            Scribe_Values.Look(ref panicDuration, "panicDuration", 8f);
            Scribe_Values.Look(ref forbidOffMap, "forbidOffMap", true);
            Scribe_Values.Look(ref ambientChatter, "ambientChatter", true);
            Scribe_Values.Look(ref hintIdle, "hintIdle", false);
            Scribe_Values.Look(ref hintHover, "hintHover", true);
            Scribe_Values.Look(ref hintHold, "hintHold", false);
            Scribe_Values.Look(ref hintPanel, "hintPanel", true);
            Scribe_Values.Look(ref throwPower, "throwPower", 1f);
            Scribe_Values.Look(ref damageMult, "damageMult", 1f);
            base.ExposeData();
        }
    }

    public class GodCursorMod : Mod
    {
        public static GodCursorSettings Settings;

        static Vector2 scroll;
        static float viewHeight;

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
            ScrollListing(inRect, ref scroll, ref viewHeight, DrawOptions);
        }

        /// <summary>Draws a listing inside a scroll view; the content height is measured from the previous frame.</summary>
        public static void ScrollListing(Rect outRect, ref Vector2 scrollPos, ref float height, Action<Listing_Standard> body)
        {
            Rect view = new Rect(0f, 0f, outRect.width - 18f, Mathf.Max(height, outRect.height));
            Widgets.BeginScrollView(outRect, ref scrollPos, view, true);
            Listing_Standard l = new Listing_Standard();
            l.Begin(view);
            body(l);
            height = l.CurHeight + 12f;
            l.End();
            Widgets.EndScrollView();
        }

        /// <summary>Shared by the mod settings page and the in-game panel.</summary>
        public static void DrawOptions(Listing_Standard l)
        {
            GodCursorSettings s = Settings;

            l.CheckboxLabeled("GC_Damage".Translate().ToString(), ref s.damage, "GC_DamageTip".Translate().ToString());
            l.CheckboxLabeled("GC_Lethal".Translate().ToString(), ref s.lethal, "GC_LethalTip".Translate().ToString());
            l.CheckboxLabeled("GC_ForbidOffMap".Translate().ToString(), ref s.forbidOffMap, "GC_ForbidOffMapTip".Translate().ToString());
            l.CheckboxLabeled("GC_FourthWall".Translate().ToString(), ref s.fourthWall, "GC_FourthWallTip".Translate().ToString());
            l.CheckboxLabeled("GC_AmbientChatter".Translate().ToString(), ref s.ambientChatter, "GC_AmbientChatterTip".Translate().ToString());
            l.CheckboxLabeled("GC_Panic".Translate().ToString(), ref s.panic, "GC_PanicTip".Translate().ToString());

            l.Gap(6f);
            l.Label("GC_ThrowPower".Translate(s.throwPower.ToString("F1")).ToString());
            s.throwPower = l.Slider(s.throwPower, 0.5f, 2.5f);
            l.Label("GC_DamageMult".Translate(s.damageMult.ToString("F1")).ToString());
            s.damageMult = l.Slider(s.damageMult, 0.25f, 3f);
            l.Label("GC_PanicDuration".Translate(Mathf.RoundToInt(s.panicDuration).ToString()).ToString());
            s.panicDuration = l.Slider(s.panicDuration, 2f, 60f);

            l.GapLine(10f);
            l.Label("GC_SectionHints".Translate().ToString());
            l.CheckboxLabeled("GC_HintIdle".Translate().ToString(), ref s.hintIdle, 0f);
            l.CheckboxLabeled("GC_HintHover".Translate().ToString(), ref s.hintHover, 0f);
            l.CheckboxLabeled("GC_HintHold".Translate().ToString(), ref s.hintHold, 0f);
            l.CheckboxLabeled("GC_HintPanel".Translate().ToString(), ref s.hintPanel, 0f);
        }
    }
}
