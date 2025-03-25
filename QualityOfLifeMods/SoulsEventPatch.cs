using HarmonyLib;
using UnityEngine;
using QualityOfLifeMods;
using System.Linq;
using BepInEx.Logging;

namespace QualityOfLifeMods {

    [HarmonyPatch(typeof(SoulsBonus), "OnEventStart")]
    public class SoulsEventStart
    {
        [HarmonyPostfix]
        public static void OnSoulsEventStart()
        {
            Plugin.Log.LogDebug("Souls event detected!");

            if (ChestExtinctionPurchaser.Instance != null)
            {
                if (ChestExtinctionPurchaser.Instance.ChestExtinctionPurchaserEnabled)
                {
                    Plugin.Log.LogDebug("Turning off Chest Extinction");
                    ChestExtinctionPurchaser.Instance.DeactivateChestExtinction();
                }
            }
            else
            {
                Plugin.Log.LogError("ChestExtinctionPurchaser instance is null!");
            }
        }
    }

    [HarmonyPatch(typeof(SoulsBonus), "OnEventEnd")]
    public class SoulsEventEnd
    {
        [HarmonyPostfix]
        public static void OnSoulsEventEnd()
        {
            Plugin.Log.LogDebug("Souls event finished!");

            if (ChestExtinctionPurchaser.Instance != null)
            {
                if (ChestExtinctionPurchaser.Instance.ChestExtinctionPurchaserEnabled)
                {
                    Plugin.Log.LogDebug("Turning on Chest Extinction");
                    ChestExtinctionPurchaser.Instance.ActivateChestExtinction();
                }
            }
            else
            {
                Plugin.Log.LogError("ChestExtinctionPurchaser instance is null!");
            }
        }
    }
}