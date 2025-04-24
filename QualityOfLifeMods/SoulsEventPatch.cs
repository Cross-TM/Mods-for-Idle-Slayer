using HarmonyLib;
using UnityEngine;
using MelonLoader;
using Il2Cpp;

namespace QualityOfLifeMods {

    [HarmonyPatch(typeof(SoulsBonus), "OnEventStart")]
    public class SoulsEventStart
    {
        [HarmonyPostfix]
        public static void OnSoulsEventStart()
        {
            if (Debug.isDebugBuild)
                Melon<Plugin>.Logger.Msg("Souls event detected!");

            if (ChestExtinctionPurchaser.Instance != null)
            {
                if (ChestExtinctionPurchaser.Instance.ChestExtinctionPurchaserEnabled)
                {
                    if (Debug.isDebugBuild)
                        Melon<Plugin>.Logger.Msg("Turning off Chest Extinction");
                    ChestExtinctionPurchaser.Instance.DeactivateChestExtinction();
                }
            }
            else
            {
                if (Debug.isDebugBuild)
                    Melon<Plugin>.Logger.Msg("ChestExtinctionPurchaser instance is null!");
            }
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

            if (ChestExtinctionPurchaser.Instance != null)
            {
                if (ChestExtinctionPurchaser.Instance.ChestExtinctionPurchaserEnabled)
                {
                    if (Debug.isDebugBuild)
                        Melon<Plugin>.Logger.Msg("Turning on Chest Extinction");
                    ChestExtinctionPurchaser.Instance.ActivateChestExtinction();
                }
            }
            else
            {
                if (Debug.isDebugBuild)
                    Melon<Plugin>.Logger.Msg("ChestExtinctionPurchaser instance is null!");
            }
        }
    }
}