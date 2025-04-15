using HarmonyLib;
using UnityEngine;
using MelonLoader;
using Il2Cpp;
using IdleSlayerMods.Common.Extensions;


namespace NoSpecialBoxesMode
{
/*    // Regular Random Boxes - Only Logs
    [HarmonyPatch(typeof(RandomBox), "OnObjectSpawn")]
    public class Patch_RandomBox_OnObjectSpawn
    {
        [HarmonyPrefix]
        public static void Prefix(RandomBox __instance)
        {
            if (__instance == null) return;

            Plugin.Logger.Debug($"A Random Box has spawned! Type: {__instance.type}, Location: {__instance.transform.position}");

            if (NoSpecialBoxes.Instance != null)
            {
                //NoSpecialBoxes.Instance.HandleBoxSpawn(__instance);
            }
            else
            {
                Plugin.Logger.Debug("NoSpecialBoxes instance is null! Special Box logic not applied.");
            }
        }
    }

    [HarmonyPatch(typeof(SpecialRandomBox), "OnObjectSpawn")]
    public class Patch_SpecialRandomBox_OnObjectSpawn
    {
        [HarmonyPrefix]
        public static void Prefix(SpecialRandomBox __instance)
        {
            if (__instance == null) return;

            Plugin.Logger.Debug($"Special Random Box Spawned at {__instance.transform.position}");

            if (NoSpecialBoxes.Instance != null)
            {
                //NoSpecialBoxes.Instance.HandleSpecialBoxSpawn(__instance);
            }
            else
            {
                Plugin.Logger.Debug("NoSpecialBoxes instance is null! Special Box logic not applied.");
            }
        }
    }
*/
    [HarmonyPatch(typeof(BonusStartSlider), "SetRandomPuzzle")]
    public class BonusStartSliderPatch
    {
        [HarmonyPostfix]
        static void Postfix(BonusStartSlider __instance)
        {
            NoSpecialBoxes.Instance.SetBonusSlider(__instance);
            Plugin.Logger.Debug("Detected BonusStartSlider creation.");
        }
    }
}
