using HarmonyLib;
using Il2Cpp;

namespace AutoRageMode;

[HarmonyPatch(typeof(Horde), nameof(Horde.OnEventStart))]
public class HordeEventPatch
{
    [HarmonyPostfix]
    public static void OnHordeEventStart()
    {
        Plugin.DLog("Horde event detected!");

        AutoRage autoRage = AutoRage.Instance;
        if (autoRage == null)
        {
            Plugin.DLog("AutoRageMode instance is null!");
            return;
        }

        if (autoRage.ShouldResetRageOnMegaHorde() && autoRage.IsMegaHordeActive())
        {
            Plugin.DLog("Mega Horde detected - applying the additional Mega Horde settings.");
            autoRage.ActivateForMegaHorde();
            return;
        }

        if (autoRage.IsSoulsHordeModeEnabled())
        {
            Plugin.DLog("Horde event occurred during SoulsHordeMode - flagging for Auto Rage.");
            autoRage.SetHordeEventActive();
        }
        else if (autoRage.IsHordeModeEnabled())
        {
            Plugin.DLog("HordeOnlyMode is ON - Activating Rage Mode for Horde event.");
            autoRage.ActivateRageModeDelayed();
        }
    }
}

[HarmonyPatch(typeof(SoulsBonus), nameof(SoulsBonus.OnEventStart))]
public class SoulsEventStart
{
    [HarmonyPostfix]
    public static void OnSoulsEventStart(SoulsBonus __instance)
    {
        Plugin.DLog("Souls event detected!");
        AutoRage.Instance?.SetActiveEvent(__instance);
    }
}

[HarmonyPatch(typeof(SoulsBonus), nameof(SoulsBonus.OnEventEnd))]
public class SoulsEventEnd
{
    [HarmonyPostfix]
    public static void OnSoulsEventEnd()
    {
        Plugin.DLog("Souls event finished!");
        AutoRage.Instance?.ClearActiveEvent();
    }
}
