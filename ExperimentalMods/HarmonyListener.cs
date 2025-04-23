using System;
using HarmonyLib;
using System.Reflection;
using Il2Cpp; // your IL2CPP namespace
using Il2CppSystem;

namespace ExperimentalMods
{

/*    // Patches for PlayerInventory methods/properties.
    [HarmonyPatch]
    public static class PlayerInventoryPatches
    {
        // Patch PlayerInventory.AddCoins(double coins)
        [HarmonyPatch(typeof(PlayerInventory), "AddCoins")]
        [HarmonyPrefix]
        public static void AddCoinsPrefix(double coins)
        {
            if (!ExperimentalMod.Instance.LoggerRunning)
                return;
            Plugin.Logger.Msg($"PlayerInventory.AddCoins called with coins: {coins}");
        }

        // Patch PlayerInventory.CalculateValues()
        [HarmonyPatch(typeof(PlayerInventory), "CalculateValues")]
        [HarmonyPrefix]
        public static void CalculateValuesPrefix()
        {
            if (!ExperimentalMod.Instance.LoggerRunning)
                return;
            Plugin.Logger.Msg("PlayerInventory.CalculateValues called");
        }

        // Patch PlayerInventory.CheckStats()
        [HarmonyPatch(typeof(PlayerInventory), "CheckStats")]
        [HarmonyPrefix]
        public static void CheckStatsPrefix()
        {
            if (!ExperimentalMod.Instance.LoggerRunning)
                return;
            Plugin.Logger.Msg("PlayerInventory.CheckStats called");
        }

        // Patch PlayerInventory.GetCoinValue() – log its return value in a postfix.
        [HarmonyPatch(typeof(PlayerInventory), "GetCoinValue")]
        [HarmonyPostfix]
        public static void GetCoinValuePostfix(double __result)
        {
            if (!ExperimentalMod.Instance.LoggerRunning)
                return;
            Plugin.Logger.Msg($"PlayerInventory.GetCoinValue returned: {__result}");
        }

        // Patch the setter of PlayerInventory.coins property.
        [HarmonyPatch(typeof(PlayerInventory), "set_coins")]
        [HarmonyPrefix]
        public static void SetCoinsPrefix(double value)
        {
            if (!ExperimentalMod.Instance.LoggerRunning)
                return;
            Plugin.Logger.Msg($"PlayerInventory.coins set to: {value}");
        }
    }

    // Patches for Upgrade methods/properties.
    [HarmonyPatch]
    public static class UpgradePatches
    {
        // Patch Upgrade.GetCost() to log its return value.
        [HarmonyPatch(typeof(Upgrade), "GetCost")]
        [HarmonyPostfix]
        public static void GetCostPostfix(double __result)
        {
            if (!ExperimentalMod.Instance.LoggerRunning)
                return;
            Plugin.Logger.Msg($"Upgrade.GetCost returned: {__result}");
        }

        // Patch the setter of Upgrade.bought property.
        [HarmonyPatch(typeof(Upgrade), "set_bought")]
        [HarmonyPrefix]
        public static void SetBoughtPrefix(bool value)
        {
            if (!ExperimentalMod.Instance.LoggerRunning)
                return;
            Plugin.Logger.Msg($"Upgrade.bought set to: {value}");
        }
    }
*/}
