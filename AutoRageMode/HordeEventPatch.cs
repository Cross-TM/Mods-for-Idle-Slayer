using HarmonyLib;
using UnityEngine;
using AutoRageMode;
using System.Linq;
using BepInEx.Logging;

namespace AutoRageMode;
[HarmonyPatch(typeof(Horde), "OnEventStart")]
public class HordeEventPatch
{
    [HarmonyPostfix]
    public static void OnHordeEventStart()
    {
        Plugin.Log.LogDebug("Horde event detected!");

        if (AutoRage.Instance != null)
        {
            // If SoulsHordeMode is enabled and a Souls event is active, flag it to trigger Rage
            if (AutoRage.Instance.IsSoulsHordeModeEnabled())
            {
                Plugin.Log.LogDebug("Horde event occurred during SoulsHordeMode - flagging for Auto Rage.");
                AutoRage.Instance.SetHordeEventActive();
            }
            // If SoulsHordeMode is OFF, trigger Rage immediately when a Horde event happens
            else if (AutoRage.Instance.IsHordeModeEnabled())
            {
                Plugin.Log.LogDebug("HordeOnlyMode is ON - Activating Rage Mode for Horde event.");
                AutoRage.Instance.ActivateRageModeDelayed();
            }
        }
        else
        {
            Plugin.Log.LogError("AutoRageMode instance is null!");
        }
    }
}


[HarmonyPatch(typeof(SoulsBonus), "OnEventStart")]
public class SoulsEventStart
{
    [HarmonyPostfix]
    public static void OnSoulsEventStart(SoulsBonus __instance) // Capture instance
    {
        Plugin.Log.LogDebug("Souls event detected!");

        AutoRage.Instance?.SetActiveEvent(__instance); // Pass instance of SoulsBonus
    }
}

[HarmonyPatch(typeof(SoulsBonus), "OnEventEnd")]
public class SoulsEventEnd
{
    [HarmonyPostfix]
    public static void OnSoulsEventEnd()
    {
        Plugin.Log.LogDebug("Souls event finished!");
        AutoRage.Instance?.ClearActiveEvent();
    }
}
