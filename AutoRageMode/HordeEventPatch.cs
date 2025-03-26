using HarmonyLib;
using UnityEngine;
using MelonLoader;
using Il2Cpp;

namespace AutoRageMode;

[HarmonyPatch(typeof(Horde), "OnEventStart")]
public class HordeEventPatch
{
    [HarmonyPostfix]
    public static void OnHordeEventStart()
    {
        if (Debug.isDebugBuild)
            Melon<Plugin>.Logger.Msg("Horde event detected!");

        if (AutoRage.Instance != null)
        {
            // If SoulsHordeMode is enabled and a Souls event is active, flag it to trigger Rage
            if (AutoRage.Instance.IsSoulsHordeModeEnabled())
            {
                if (Debug.isDebugBuild)
                    Melon<Plugin>.Logger.Msg("Horde event occurred during SoulsHordeMode - flagging for Auto Rage.");
                AutoRage.Instance.SetHordeEventActive();
            }
            // If SoulsHordeMode is OFF, trigger Rage immediately when a Horde event happens
            else if (AutoRage.Instance.IsHordeModeEnabled())
            {
                if (Debug.isDebugBuild)
                    Melon<Plugin>.Logger.Msg("HordeOnlyMode is ON - Activating Rage Mode for Horde event.");
                AutoRage.Instance.ActivateRageModeDelayed();
            }
        }
        else
        {
            if (Debug.isDebugBuild)
                Melon<Plugin>.Logger.Msg("AutoRageMode instance is null!");
        }
    }
}


[HarmonyPatch(typeof(SoulsBonus), "OnEventStart")]
public class SoulsEventStart
{
    [HarmonyPostfix]
    public static void OnSoulsEventStart(SoulsBonus __instance) // Capture instance
    {
        if (Debug.isDebugBuild)
            Melon<Plugin>.Logger.Msg("Souls event detected!");

        AutoRage.Instance?.SetActiveEvent(__instance); // Pass instance of SoulsBonus
    }
}

[HarmonyPatch(typeof(SoulsBonus), "OnEventEnd")]
public class SoulsEventEnd
{
    [HarmonyPostfix]
    public static void OnSoulsEventEnd()
    {
        if (Debug.isDebugBuild)
            Melon<Plugin>.Logger.Msg("Souls event finished!");
        AutoRage.Instance?.ClearActiveEvent();
    }
}
