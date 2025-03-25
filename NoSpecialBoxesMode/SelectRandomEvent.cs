using HarmonyLib;
using UnityEngine;

namespace NoSpecialBoxesMode
{
    // Regular Random Boxes - Only Logs
    [HarmonyPatch(typeof(RandomBox), "OnObjectSpawn")]
    public class Patch_RandomBox_OnObjectSpawn
    {
        [HarmonyPostfix]
        public static void Postfix(RandomBox __instance)
        {
            if (__instance == null) return;

            Plugin.Log.LogDebug($"A Random Box has spawned! Type: {__instance.type}, Location: {__instance.transform.position}");

/*            if (NoSpecialBoxes.Instance != null)
            {
                NoSpecialBoxes.Instance.HandleSpecialBoxSpawn(__instance);
            }
            else
            {
                Plugin.Log.LogError("NoSpecialBoxes instance is null! Special Box logic not applied.");
            }
*/        }
    }

    [HarmonyPatch(typeof(SpecialRandomBox), "OnObjectSpawn")]
    public class Patch_SpecialRandomBox_OnObjectSpawn
    {
        [HarmonyPostfix]
        public static void Postfix(SpecialRandomBox __instance)
        {
            if (__instance == null) return;

            Plugin.Log.LogDebug($"Special Random Box Spawned at {__instance.transform.position}");

            if (NoSpecialBoxes.Instance != null)
            {
                NoSpecialBoxes.Instance.HandleSpecialBoxSpawn(__instance);
            }
            else
            {
                Plugin.Log.LogError("NoSpecialBoxes instance is null! Special Box logic not applied.");
            }
        }
    }

    [HarmonyPatch(typeof(BonusStartSlider), "SetRandomPuzzle")]
    public class BonusStartSliderPatch
    {
        [HarmonyPostfix]
        static void Postfix(BonusStartSlider __instance)
        {
            NoSpecialBoxes.Instance.SetBonusSlider(__instance);
            Plugin.Log.LogDebug("Detected BonusStartSlider creation.");
        }
    }
}
