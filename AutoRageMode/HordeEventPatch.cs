using HarmonyLib;
using UnityEngine;
using MelonLoader;
using Il2Cpp;
using System.Diagnostics;

namespace AutoRageMode;

[HarmonyPatch(typeof(Horde), "OnEventStart")]
public class HordeEventPatch
{

    [HarmonyPostfix]
    public static void OnHordeEventStart()
    {
        Plugin.DLog("Horde event detected!");

        if (AutoRage.Instance != null)
        {
            // If SoulsHordeMode is enabled and a Souls event is active, flag it to trigger Rage
            if (AutoRage.Instance.IsSoulsHordeModeEnabled())
            {
                Plugin.DLog("Horde event occurred during SoulsHordeMode - flagging for Auto Rage.");
                AutoRage.Instance.SetHordeEventActive();
            }
            // If SoulsHordeMode is OFF, trigger Rage immediately when a Horde event happens
            else if (AutoRage.Instance.IsHordeModeEnabled())
            {
                Plugin.DLog("HordeOnlyMode is ON - Activating Rage Mode for Horde event.");
                AutoRage.Instance.ActivateRageModeDelayed();
            }
        }
        else
        {
            Plugin.DLog("AutoRageMode instance is null!");
        }
    }
}


[HarmonyPatch(typeof(SoulsBonus), "OnEventStart")]
public class SoulsEventStart
{
    [HarmonyPostfix]
    public static void OnSoulsEventStart(SoulsBonus __instance) // Capture instance
    {
        Plugin.DLog("Souls event detected!");

        AutoRage.Instance?.SetActiveEvent(__instance); // Pass instance of SoulsBonus
    }
}

[HarmonyPatch(typeof(SoulsBonus), "OnEventEnd")]
public class SoulsEventEnd
{
    [HarmonyPostfix]
    public static void OnSoulsEventEnd()
    {
        Plugin.DLog("Souls event finished!");
        AutoRage.Instance?.ClearActiveEvent();
    }
}
