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
        Plugin.Log.LogInfo("Horde event detected!");

        if (AutoRage.Instance != null)
        {
            Plugin.Log.LogInfo("AutoRageMode instance found. Activating Rage Mode...");
            AutoRage.Instance.ActivateRageModeForHorde();
        }
        else
        {
            Plugin.Log.LogError("AutoRageMode instance is null!");
        }
    }
}