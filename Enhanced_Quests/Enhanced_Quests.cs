using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Enhanced_Quests;

public class Enhanced_Quests : MonoBehaviour
{
    public static Enhanced_Quests Instance { get; private set; }

    private CraftableItems _craftableItems;
    private PermanentCraftableItem weeklyQuests;
    private PermanentCraftableItem dailyQuests;


    private DailyQuestsManager _dailyQuestsManager;
    private WeeklyQuestsManager _weeklyQuestsManager;
    private DailyQuestReroll _dailyQuestReroll;
    private WeeklyQuestReroll _weeklyQuestReroll;

    private PortalButton _portalButton;

    private GameObject _gameObject;
    private Il2CppSystem.Collections.Generic.List<Quest> _questsList;

    private bool questsChecking = false;
    public void Awake()
    {
        Instance = this;

        _portalButton = PortalButton.instance;
        _dailyQuestReroll = DailyQuestReroll.instance;
        _weeklyQuestReroll = WeeklyQuestReroll.instance;

        _dailyQuestsManager = DailyQuestsManager.instance;
        _weeklyQuestsManager = WeeklyQuestsManager.instance;

        _craftableItems = CraftableItems.list;
        weeklyQuests = _craftableItems.WeeklyQuests;
        dailyQuests = _craftableItems.DailyQuests;

    }

    public void Start()
    {
        CheckToAllowQuestReroll();
        MelonCoroutines.Start(InitialiseQuests());

        CheckToRegenerate();
    }

    public void Update()
    {
        bool isRunner = GameState.IsRunner();

        if (isRunner && !questsChecking && Plugin.Settings.AutoClaimQuests.Value)
        {
            questsChecking = true;
            CheckQuestsForClaims();
        }

    }
    public IEnumerator InitialiseQuests()
    {
        for (int i = 0; i < 20; i++)
        {
            if (HasQuestList())
            {
                CheckToRegenerate();
                yield break;
            }

            yield return new WaitForSeconds(0.25f);
        }
    }

    private bool HasQuestList()
    {
        RefreshQuestList();
        return _questsList != null && _questsList.Count > 0;
    }

    public void RefreshQuestList()
    {
        _gameObject = GameObject.Find("UIManager/Safe Zone/Shop Panel/Wrapper/Quests/Quests");
        if (_gameObject == null) return;

        var questsList = _gameObject.GetComponent<QuestsList>();
        if (questsList == null) return;

        _questsList = questsList.lastScrollListData;
    }


    private void CheckQuestsForClaims()
    {
        RefreshQuestList();
        if (_questsList != null && _questsList.Count > 0)
        {
            foreach (Quest quest in _questsList)
            {
                if (quest.CanBeClaimed())
                {
                    quest.Claim();
                }
            }
        }
        questsChecking = false;
    }

    public void CheckToRegenerate()
    {
        RefreshQuestList();
        if (_questsList != null && _questsList.Count > 0)
        {
            int dailyQuestsCount = 0;
            int weeklyQuestsCount = 0;

            foreach (var quest in _questsList)
            {
                var questTypeName = quest.GetIl2CppType().FullName;

                if (questTypeName == "WeeklyQuest")
                    weeklyQuestsCount++;
                else if (questTypeName == "DailyQuest")
                    dailyQuestsCount++;

            }

            if (dailyQuestsCount == 0 && dailyQuests.IsActive() && Plugin.Settings.ResetDailies.Value)
                _dailyQuestsManager.RegenerateDailys();

            if (weeklyQuestsCount == 0 && weeklyQuests.IsActive() && Plugin.Settings.ResetWeeklies.Value)
                _weeklyQuestsManager.RegenerateWeeklies();
        }
        RefreshQuestList();
    }

    [HarmonyPatch(typeof(DailyQuest), "Claim")]
    public static class Patch_DailyQuestClaim
    {
        static void Postfix(Quest __instance)
        {
            if (__instance == null) return;

            Enhanced_Quests.Instance?.Invoke(
              nameof(Enhanced_Quests.CheckToRegenerate), 2f);
        }
    }

    [HarmonyPatch(typeof(WeeklyQuest), "Claim")]
    public class Patch_WeeklyQuestClaim
    {
        static void Postfix(Quest __instance)
        {
            if (__instance == null) return;
            Enhanced_Quests.Instance?.Invoke(
              nameof(Enhanced_Quests.CheckToRegenerate), 2f);
        }
    }

    [HarmonyPatch(typeof(DailyQuestReroll), "RewardForShowing")]
    public class Patch_DailyQuestRerollReward
    {
        static void Postfix()
        {
            Enhanced_Quests.Instance?.CheckToAllowQuestReroll();
        }
    }

    [HarmonyPatch(typeof(WeeklyQuestReroll), "RewardForShowing")]
    public class Patch_WeeklyQuestRerollReward
    {
        static void Postfix()
        {
            Enhanced_Quests.Instance?.CheckToAllowQuestReroll();
        }
    }

    [HarmonyPatch(typeof(Button), "Press")]
    public class Patch_PortalButton
    {
        static void Postfix(Button __instance)
        {
            if (__instance.name != "Portal Button" && __instance.name != "Portal Button(Clone)") return;
            Enhanced_Quests.Instance?.CheckToResetPortal();
        }
    }

    public void CheckToResetPortal()
    {
        if (_portalButton.currentCd > 0 && Plugin.Settings.ResetPortal.Value)
            _portalButton.currentCd = 0;
    }

    public void CheckToAllowQuestReroll()
    {
        if (Plugin.Settings.ResetReroll.Value)
        {
            _dailyQuestReroll.rerollEnabled = true;
            _weeklyQuestReroll.rerollEnabled = true;
        }
    }


}
