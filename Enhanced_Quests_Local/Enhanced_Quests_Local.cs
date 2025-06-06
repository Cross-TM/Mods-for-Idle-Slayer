using HarmonyLib;
using Il2Cpp;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using MelonLoader;
using System.Linq;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace Enhanced_Quests_Local;

public class Enhanced_Quests : MonoBehaviour
{
    public static Enhanced_Quests Instance { get; private set; }

    private CraftableItems _craftableItems;
    private PermanentCraftableItem weeklyQuests;
    private PermanentCraftableItem dailyQuests;

    private QuestManager _questManager;
    private DailyQuestsManager _dailyQuestsManager;
    private WeeklyQuestsManager _weeklyQuestsManager;
    private DailyQuestReroll _dailyQuestReroll;
    private WeeklyQuestReroll _weeklyQuestReroll;

    private PlayerInventory _playerInventory;
    private MapController _mapController;

    private Quest ChestKeyQuest;
    private GameObject _gameObject;
    private PortalButton _portalButton;
    public BaseMap newMap;

    private Il2CppSystem.Collections.Generic.List<Quest> _questsList;

    private bool questsChecking = false;
    public bool claimingQuest = false;

    private bool DailiesChanged;
    private bool WeekliesChanged;
    private bool checkingDailies;
    private bool checkingWeeklies;

    private int dailiesRerolled;
    private int weekliesRerolled;

    public void Awake()
    {
        Instance = this;

        _portalButton = PortalButton.instance;
        _dailyQuestReroll = DailyQuestReroll.instance;
        _weeklyQuestReroll = WeeklyQuestReroll.instance;

        _dailyQuestsManager = DailyQuestsManager.instance;
        _weeklyQuestsManager = WeeklyQuestsManager.instance;
        _questManager = QuestManager.instance;

        _playerInventory = PlayerInventory.instance;
        _mapController = MapController.instance;

        _craftableItems = CraftableItems.list;
        weeklyQuests = _craftableItems.WeeklyQuests;
        dailyQuests = _craftableItems.DailyQuests;

    }

    public void Start()
    { 
        CheckToRegenerate();
        CheckToRerollQuests();

        foreach (Quest quest in _playerInventory.allQuests)
        {
            if (quest.name == "quest_the_key_to_success")
            {
                ChestKeyQuest = quest;
                break;
            }
        }
    }

    public void RefreshQuestList() 
    { 
        _gameObject = GameObject.Find("UIManager/Safe Zone/Shop Panel/Wrapper/Quests/Quests");
        _questsList = _gameObject.GetComponent<QuestsList>().lastScrollListData;    
    }
    public void Update()
    {
        if (!questsChecking)
        {
            questsChecking = true;
            CheckQuests();
        }

        if (Input.GetKeyDown(KeyCode.Alpha0)) 
        {
            //            RerollDailyQuests();
            //            RerollWeeklyQuests();
            MelonCoroutines.Start(RerollQuestDelay("Daily"));
            MelonCoroutines.Start(RerollQuestDelay("Weekly"));
        }
    }

    private void CheckQuests()
    {
        RefreshQuestList();
        if (_questsList != null && _questsList.Count > 0)
        {
            foreach (Quest quest in _questsList)
            {
                if (quest.CanBeClaimed())
                {
                    quest.Claim();
                    CheckToRegenerate();
                }
            }
        }
        questsChecking = false;
    }

    public void CheckToRegenerate()
    {
        RefreshQuestList();

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
        
        RefreshQuestList();
    }


    public IEnumerator RerollQuestDelay(String type)
    {
        yield return new WaitForSeconds(2f);
        RerollQuests(type);
    }

    public void RerollQuests(String type)
    {
        if (type == "Daily")
        {
            if (!checkingDailies)
            {
                checkingDailies = true;
                MelonCoroutines.Start(CheckRollQuests(type));
            }
        }
        else if (type == "Weekly")
        {
            if (!checkingWeeklies)
            {
                checkingWeeklies = true;
                MelonCoroutines.Start(CheckRollQuests(type));
            }
        }
    }

/*    public void RerollDailyQuests()
    {
        dailiesRerolled = 0;

        if (!checkingDailies)
        {
            checkingDailies = true;
            MelonCoroutines.Start(checkDailies());
        }
    }


    public void RerollWeeklyQuests()
    {
        if (!checkingWeeklies)
        {
            checkingWeeklies = true;
            MelonCoroutines.Start(checkWeeklies());
        }
    }
*/
    private IEnumerator CheckRollQuests(String type)
    {
        bool QuestsChanged = false;

        foreach (Quest q in _questsList)
        {
            var questTypeName = q.GetIl2CppType().FullName;
            if ((questTypeName == "DailyQuest" && type == "Daily") || (questTypeName == "WeeklyQuest" && type == "Weekly"))
            {
                if (!CanBeAutoCompleted(q))
                {
                    RerollQuest(q,type);
                    QuestsChanged = true;
                }
            }
        }

        yield return new WaitForSeconds(0.5f);

        if (type == "Daily")
        {
            checkingDailies = false;
        }
        else if (type == "Weekly")
        {
            checkingWeeklies = false;
        }

        if (QuestsChanged)
            RerollQuests(type);
        else if (type == "Daily")
            dailiesRerolled = 0;
    }

    private bool CanBeAutoCompleted(Quest quest)
    {
        QuestType questType = quest.questType;

        switch (questType)
        {
            //Quick Quests
            case QuestType.CompleteDailyQuests:
            case QuestType.HitRandomSilverBoxes:
            case QuestType.PickUpCoins:
            case QuestType.PickUpGemstones:
                return true;

            //Chest Hunt Daily vs Weekly and if relevant quest is completed
            case QuestType.ChestHuntChests:
                {
                    var questTypeName = quest.GetIl2CppType().FullName;
                    if (questTypeName == "DailyQuest" && ChestKeyQuest.isClaimed) return true; else return false;
                }

            //Kill With Rage Mode Daily vs Weekly
            case QuestType.KillEnemiesWithRageMode:
                {
                    var questTypeName = quest.GetIl2CppType().FullName;
                    if (questTypeName == "DailyQuest") return true; else return false;
                }

            //Conditional Quests
            case QuestType.GetMaterials:
            case QuestType.KillEnemies:
            case QuestType.KillGiants:
                return ConditionalQuestCheck(quest);

            //Medium Quests
            case QuestType.BonusStage:
            case QuestType.BonusStageSections:
            case QuestType.Boost:
                return (dailiesRerolled > 15);

            //Long Quests
            case QuestType.HitRandomBoxes:
            case QuestType.UseRageMode:
                return (dailiesRerolled > 20);

            //Manual Quest
            case QuestType.CraftTemporaryItems:
            case QuestType.GoThroughPortals:
            case QuestType.WindDashKills:
                return false;

            default:
                return true; // Default case for unhandled quest types
        }
    }

    private bool ConditionalQuestCheck(Quest quest)
    {
        BaseMap currentMap;

        if (GameState.IsRunner())
            currentMap = _mapController.selectedMap as BaseMap;
        else
            currentMap = new BaseMap(_mapController.lastRunnerMap.Pointer);

        if (quest.questType == QuestType.KillGiants)
        {
            foreach (Enemy enemy in currentMap.currentStageEnemies)
            {
                if (enemy.isGiant)
                {
                    return true;
                }
            }
        }
        else if (quest.questType == QuestType.KillEnemies)
        {
            if (quest.enemyToKill.name == "enemy_king_goblin" || quest.enemyToKill.name == "enemy_soul_goblin")
            {
                return (dailiesRerolled > 15);
            }

            foreach (Enemy enemy in currentMap.currentStageEnemies)
            {
                if (enemy == quest.enemyToKill || _questManager.EnemyFromSameStageOrBelow(quest.enemyToKill, enemy))
                {
                    return true;
                }
            }
        }
        else if (quest.questType == QuestType.GetMaterials)
        { 
            foreach (Enemy enemy in currentMap.currentStageEnemies)
            {
                if (enemy.drop != null)
                    return true;
            }
        }


        return false;
    }

    private void RerollQuest(Quest quest, String type)
    {
        if (type == "Daily")
        {
            var dq = new DailyQuest(quest.Pointer);

            _dailyQuestReroll.PrepareReroll(dq);
            _dailyQuestReroll.RewardForShowing();
            dailiesRerolled++;
        }
        else if (type == "Weekly")
        {
            var wq = new WeeklyQuest(quest.Pointer);

            _weeklyQuestReroll.PrepareReroll(wq);
            _weeklyQuestReroll.RewardForShowing();
        }
    }

    private void RerollDailyQuest(DailyQuest quest) 
    {
        _dailyQuestReroll.PrepareReroll(quest);
        _dailyQuestReroll.RewardForShowing();
        DailiesChanged = true;
        dailiesRerolled++;
    }

    private void RerollWeeklyQuest(WeeklyQuest quest)
    {
        _weeklyQuestReroll.PrepareReroll(quest);
        _weeklyQuestReroll.RewardForShowing();
        WeekliesChanged = true;
    }

    [HarmonyPatch(typeof(MapController), "ChangeMap")]
    public class Patch_MapControllerChangeMap
    {
        static void Postfix(MapController __instance)
        {
            if (__instance == null) return;
            MelonCoroutines.Start(Enhanced_Quests.Instance.RerollQuestDelay("Daily"));
        }
    }

    [HarmonyPatch(typeof(DailyQuestsManager), "RegenerateDailys")]
    public class Patch_DailyQuestsManagerRegenerateDailys
    {
        static void Postfix(DailyQuestsManager __instance)
        {
            if (__instance == null) return;
            while (!GameState.IsRunner())
            { }
            MelonCoroutines.Start(Enhanced_Quests.Instance.RerollQuestDelay("Daily"));
        }
    }
    [HarmonyPatch(typeof(WeeklyQuestsManager), "RegenerateWeeklies")]
    public class Patch_WeeklyQuestsManagerRegenerateWeeklies
    {
        static void Postfix(WeeklyQuestsManager __instance)
        {
            if (__instance == null) return;
            while (!GameState.IsRunner())
            { }
            MelonCoroutines.Start(Enhanced_Quests.Instance.RerollQuestDelay("Weekly"));
        }
    }

    [HarmonyPatch(typeof(DailyQuestsManager), "AddQuests")]
    public class Patch_DailyQuestsManagerAddQuests
    {
        static void Postfix(DailyQuestsManager __instance)
        {
            if (__instance == null) return;
            MelonCoroutines.Start(Enhanced_Quests.Instance.RerollQuestDelay("Daily"));

        }
    }

    [HarmonyPatch(typeof(WeeklyQuestsManager), "AddQuests")]
    public class Patch_WeeklyQuestsManagerAddQuests
    {
        static void Postfix(WeeklyQuestsManager __instance)
        {
            if (__instance == null) return;
            MelonCoroutines.Start(Enhanced_Quests.Instance.RerollQuestDelay("Weekly"));
        }
    }

    [HarmonyPatch(typeof(DailyQuest), "Claim")]
    public static class Patch_DailyQuestClaim
    {
        static void Postfix(Quest __instance)
        {
            if (__instance == null) return;
            while (!GameState.IsRunner())
            { }

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
            while (!GameState.IsRunner())
            { }

            Enhanced_Quests.Instance?.Invoke(
                nameof(Enhanced_Quests.CheckToRegenerate),2f);
        }
    }

    [HarmonyPatch(typeof(DailyQuestReroll), "RewardForShowing")]
    public class Patch_DailyQuestRerollReward
    {
        static void Postfix()
        {
            Enhanced_Quests.Instance?.CheckToRerollQuests();
        }
    }

    [HarmonyPatch(typeof(WeeklyQuestReroll), "RewardForShowing")]
    public class Patch_WeeklyQuestRerollReward
    {
        static void Postfix()
        {
            Enhanced_Quests.Instance?.CheckToRerollQuests();
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

    public void CheckToRerollQuests()
    {
        if (Plugin.Settings.ResetReroll.Value)
        {
            _dailyQuestReroll.rerollEnabled = true;
            _weeklyQuestReroll.rerollEnabled = true;
        }
    }


}
