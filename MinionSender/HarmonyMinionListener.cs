using HarmonyLib;
using UnityEngine;
using MelonLoader;
using Il2Cpp;

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
                if (Debug.isDebugBuild)
                    Melon<Plugin>.Logger.Msg("Some minions are not working!");
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
                if (Debug.isDebugBuild)
                    Melon<Plugin>.Logger.Msg("Minions are ready to be claimed!");
                MinionSender.Instance.ClaimMinions();
            }
        }
    }
}