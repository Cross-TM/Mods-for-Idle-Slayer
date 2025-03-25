using HarmonyLib;
using UnityEngine;
using QualityOfLifeMods;
using System.Linq;
using BepInEx.Logging;

namespace QualityOfLifeMods
{

    [HarmonyPatch(typeof(MinionManager), "AreAllMinionsWorking")]
    public class MinionsWorkingPatch
    {
        [HarmonyPostfix]
        public static void OnMinionsWorkingCheck(bool __result)
        {
            if (!__result && MinionSender.Instance.MinionSenderEnabled)
            {
                Plugin.Log.LogDebug("Some minions are not working!");
                MinionSender.Instance.SendMinions();
            }
        }
    }

    [HarmonyPatch(typeof(MinionManager), "AreMinionsToBeClaimed")]
    public class MinionsToBeClaimedPatch
    {
        [HarmonyPostfix]
        public static void OnMinionsToBeClaimed(bool __result)
        {
            if (__result && MinionSender.Instance.MinionSenderEnabled)
            {
                Plugin.Log.LogDebug("Minions are ready to be claimed!");
                MinionSender.Instance.ClaimMinions();
            }
        }
    }
}