using RimWorld;
using UnityEngine;
using Verse;

namespace GodCursor
{
    /// <summary>Small draggable control panel. Does not pause the game and does not block the map.</summary>
    public class Window_GodCursor : Window
    {
        Vector2 scroll;
        float viewHeight;

        public override Vector2 InitialSize
        {
            get { return new Vector2(360f, 620f); }
        }

        public Window_GodCursor()
        {
            layer = WindowLayer.GameUI;
            draggable = true;
            doCloseX = true;
            doCloseButton = false;
            closeOnClickedOutside = false;
            closeOnAccept = false;
            closeOnCancel = false;
            absorbInputAroundWindow = false;
            preventCameraMotion = false;
            forcePause = false;
            focusWhenOpened = false;
            onlyOneOfTypeAllowed = true;
        }

        protected override void SetInitialSizeAndPosition()
        {
            base.SetInitialSizeAndPosition();
            windowRect.x = UI.screenWidth - InitialSize.x - 20f;
            windowRect.y = Mathf.Max(20f, (UI.screenHeight - InitialSize.y) / 2f - 40f);
        }

        public override void DoWindowContents(Rect inRect)
        {
            GodCursorMod.ScrollListing(inRect, ref scroll, ref viewHeight, DrawContents);
        }

        void DrawContents(Listing_Standard l)
        {
            Text.Font = GameFont.Medium;
            l.Label("GC_PanelTitle".Translate().ToString());
            Text.Font = GameFont.Small;

            string status = (Ctrl.Active ? "GC_StatusOn" : "GC_StatusOff").Translate().ToString();
            l.Label("GC_Status".Translate(status).ToString());

            string btn = (Ctrl.Active ? "GC_BtnDisable" : "GC_BtnEnable").Translate().ToString();
            if (l.ButtonText(btn))
            {
                Ctrl.SetActive(!Ctrl.Active);
            }
            if (l.ButtonText("GC_CalmAll".Translate().ToString()))
            {
                Ctrl.CalmAll();
            }

            l.GapLine(10f);
            GodCursorMod.DrawOptions(l);

            if (GodCursorMod.Settings.hintPanel)
            {
                l.GapLine(10f);
                Text.Font = GameFont.Tiny;
                l.Label("GC_Hints".Translate().ToString());
                Text.Font = GameFont.Small;
            }
        }

        public override void PostClose()
        {
            base.PostClose();
            if (GodCursorMod.Settings != null) GodCursorMod.Settings.Write();
        }
    }
}
