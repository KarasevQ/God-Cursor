using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GodCursor
{
    [StaticConstructorOnStartup]
    public static class GodCursorPatcher
    {
        static GodCursorPatcher()
        {
            Harmony h = new Harmony("godcursor.rimworld.mod");

            // 1. Our click handling replaces vanilla selection while the mode is on.
            Apply(h, AccessTools.Method(typeof(Selector), "HandleMapClicks"), "SelectorPrefix", null);

            // 2. A pawn that is held or flying must not act on its own.
            Apply(h, AccessTools.Method(typeof(Pawn), "Tick"), "PawnTickPrefix", null);
            Apply(h, AccessTools.Method(typeof(Pawn), "TickRare"), "PawnTickPrefix", null);
            Apply(h, AccessTools.Method(typeof(Pawn), "TickInterval"), "PawnTickPrefix", null);

            // 3. Where the pawn is drawn.
            Apply(h, AccessTools.PropertyGetter(typeof(Pawn_DrawTracker), "DrawPos"), null, "TrackerDrawPosPostfix");
            Apply(h, AccessTools.PropertyGetter(typeof(Pawn), "DrawPos"), null, "PawnDrawPosPostfix");
        }

        static void Apply(Harmony h, MethodBase target, string prefix, string postfix)
        {
            if (target == null)
            {
                Log.Warning("[GodCursor] Could not find a method to patch (prefix=" + prefix + ", postfix=" + postfix + "). Some features may not work.");
                return;
            }
            try
            {
                HarmonyMethod pre = prefix != null ? new HarmonyMethod(typeof(GodCursorPatcher).GetMethod(prefix)) : null;
                HarmonyMethod post = postfix != null ? new HarmonyMethod(typeof(GodCursorPatcher).GetMethod(postfix)) : null;
                h.Patch(target, pre, post);
            }
            catch (Exception e)
            {
                Log.Error("[GodCursor] Failed to patch " + target.DeclaringType + "." + target.Name + ": " + e);
            }
        }

        // ---- patch bodies ----

        public static bool SelectorPrefix()
        {
            return !Ctrl.HandleSelectorInput();
        }

        public static bool PawnTickPrefix(Pawn __instance)
        {
            return !Ctrl.IsLocked(__instance);
        }

        public static void TrackerDrawPosPostfix(Pawn ___pawn, ref Vector3 __result)
        {
            if (!Ctrl.AnyOverride) return;
            Ctrl.ModifyDrawPos(___pawn, ref __result);
        }

        public static void PawnDrawPosPostfix(Pawn __instance, ref Vector3 __result)
        {
            if (!Ctrl.AnyOverride) return;
            Ctrl.ModifyDrawPos(__instance, ref __result);
        }
    }
}
