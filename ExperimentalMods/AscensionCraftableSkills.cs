using Il2Cpp;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using MelonLoader;
using System;
using System.Reflection;
using UnityEngine.UI;

namespace ExperimentalMods
{
    public class CraftableSkills : MonoBehaviour
    {
        public static CraftableSkills Instance { get; private set; }
        private PortalButton _portalButton;
        private CraftableItems _craftableItems;
        private PermanentCraftableItem _weeklyQuests;
        private PermanentCraftableItem _dailyQuests;

        private void Awake()
        {
            Instance = this;
            _craftableItems = CraftableItems.list;
            _weeklyQuests = _craftableItems.WeeklyQuests;
            _dailyQuests = _craftableItems.DailyQuests;
            _portalButton = PortalButton.instance;
        }
        private void LateUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                _weeklyQuests.unlocked = !_weeklyQuests.unlocked;
                Plugin.Logger.Msg($"Weekly Quests are {(_weeklyQuests.unlocked ? "Unlocked" : "Locked")}");
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                _dailyQuests.unlocked = !_dailyQuests.unlocked;
                Plugin.Logger.Msg($"Daily Quests are {(_dailyQuests.unlocked ? "Unlocked" : "Locked")}");
            }

        }
    }
}
