using RimWorld;
using UnityEngine;
using Verse;

namespace GodCursor
{
    /// <summary>Small draggable control panel. Does not pause the game and does not block the map.</summary>
    public class Window_GodCursor : Window
    {
        public override Vector2 InitialSize
        {
            get { return new Vector2(330f, 470f); }
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
            windowRect.y = 90f;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard l = new Listing_Standard();
            l.Begin(inRect);

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

            l.GapLine(10f);
            GodCursorMod.DrawOptions(l);
            l.GapLine(10f);

            Text.Font = GameFont.Tiny;
            l.Label("GC_Hints".Translate().ToString());
            Text.Font = GameFont.Small;

            l.End();
        }

        public override void PostClose()
        {
            base.PostClose();
            if (GodCursorMod.Settings != null) GodCursorMod.Settings.Write();
        }
    }
}
