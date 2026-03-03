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

    private object _rerollDailyHandle;
    private object _rerollWeeklyHandle;
    private object _checkDailyHandle;
    private object _checkWeeklyHandle;

    private bool _lastIsRunner;
    private bool DailiesChanged;
    private bool WeekliesChanged;
    private bool checkingDailies;
    private bool checkingWeeklies;
    private bool chestKeySpawned;

    private bool triggerDailies;
    private bool triggerWeeklies;
    private bool triggerRegen;

    private int dailiesRerolled;
    private int weekliesRerolled;

    private int _softRerollCap;
    private int _hardRerollCap;
    private float _rerollCheckMinutes;

    private float _rerollCheckTimer = 0f;

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

        _softRerollCap = Math.Max(Plugin.Settings.SoftRerollCap.Value, 15);
        _hardRerollCap = Math.Max(Plugin.Settings.HardRerollCap.Value, 20);
        _rerollCheckMinutes = Math.Max(Plugin.Settings.RerollCheckMinutes.Value, 5);
    }

    public void Start()
    {
        _lastIsRunner = GameState.IsRunner();

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

        foreach (var player_drop in _playerInventory.drops)
        {
            Plugin.Logger.Msg($"Player Materials - {player_drop.name}: {player_drop.amount}/{player_drop.GetMaxAmount()}");
        }

        foreach (var divinity in _playerInventory.divinities)
        {
            Plugin.Logger.Msg($"Player Divinities - {divinity.name} - Is Dark: {divinity.isDark} - Enabled: {divinity.unlocked}");
        }
    }

    public void Update()
    {
        bool isRunner = GameState.IsRunner();

        if (_lastIsRunner && !isRunner)
            StopAllQuestCoroutines();

        if (isRunner && !_lastIsRunner)
            chestKeySpawned = false;
            CheckTriggerRegen();

        _lastIsRunner = isRunner;

        if (isRunner && !questsChecking)
        {
            questsChecking = true;
            CheckQuests();
        }

        if (Input.GetKeyDown(KeyCode.Alpha0)) 
        {
            Plugin.Logger.Msg("Refreshing Quest List");

            StopAllQuestCoroutines();
            DelayedReroll("Daily");
            DelayedReroll("Weekly");
        }

        if (Input.GetKeyDown(KeyCode.Alpha9)) 
        {
            _dailyQuestReroll.RewardForShowing();
        }

        // Timer for forced reroll check
        if (Plugin.Settings.EnableForceRerollTimer.Value)
        {
            _rerollCheckTimer += Time.unscaledDeltaTime;
            if (_rerollCheckTimer >= Plugin.Settings.RerollCheckMinutes.Value * 60f)
            {
                _rerollCheckTimer = 0f;
                Plugin.Logger.Msg("Forced reroll check triggered by timer.");
                StopAllQuestCoroutines();
                DelayedReroll("Daily");
                DelayedReroll("Weekly");
            }
        }
    }

    public void SetTriggerRegen(String type)
    {
        if (type == "Daily")
            triggerDailies = true;
        else if (type == "Weekly")
            triggerWeeklies = true;
        else if (type == "Regen")
            triggerRegen = true;
    }

    public void CheckTriggerRegen()
    {
        if (triggerDailies)
        {
            triggerDailies = false;
            DelayedReroll("Daily");
        }
        
        if (triggerWeeklies)
        {
            triggerWeeklies = false;
            DelayedReroll("Weekly");
        }

        if (triggerRegen)
        {
            triggerRegen = false;
            Invoke(nameof(Enhanced_Quests.CheckToRegenerate), 2f);
        }
    }

    public void DelayedReroll(String type)
    {
        if (type == "Daily")
        {
            if (_rerollDailyHandle != null)
                MelonCoroutines.Stop(_rerollDailyHandle);
            _rerollDailyHandle = MelonCoroutines.Start(RerollQuestDelay("Daily"));
        }
        else if (type == "Weekly")
        {
            if (_rerollWeeklyHandle != null)
                MelonCoroutines.Stop(_rerollWeeklyHandle);
            _rerollWeeklyHandle = MelonCoroutines.Start(RerollQuestDelay("Weekly"));
        }
    }

    public bool PauseRoutines()
    {
        return !GameState.IsRunner() || chestKeySpawned;
    }
    public void RefreshQuestList() 
    { 
        _gameObject = GameObject.Find("UIManager/Safe Zone/Shop Panel/Wrapper/Quests/Quests");
        _questsList = _gameObject.GetComponent<QuestsList>().lastScrollListData;    
    }


    private void StopAllQuestCoroutines()
    {
        if (_rerollDailyHandle != null) { MelonCoroutines.Stop(_rerollDailyHandle); _rerollDailyHandle = null; }
        if (_rerollWeeklyHandle != null) { MelonCoroutines.Stop(_rerollWeeklyHandle); _rerollWeeklyHandle = null; }
        if (_checkDailyHandle != null) { MelonCoroutines.Stop(_checkDailyHandle); _checkDailyHandle = null; }
        if (_checkWeeklyHandle != null) { MelonCoroutines.Stop(_checkWeeklyHandle); _checkWeeklyHandle = null; }

        checkingDailies = false;
        checkingWeeklies = false;;
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
        dailiesRerolled = 0;
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
        
        if (PauseRoutines())
            yield break;
        
        RerollQuests(type);
    }

    public void RerollQuests(String type)
    {
        if (type == "Daily")
        {
            if (!checkingDailies)
            {
                checkingDailies = true;

                if (_checkDailyHandle != null)
                    MelonCoroutines.Stop(_checkDailyHandle);

                _checkDailyHandle = MelonCoroutines.Start(CheckRollQuests(type));
            }
        }
        else if (type == "Weekly")
        {
            if (!checkingWeeklies)
            {
                checkingWeeklies = true;

                if (_checkWeeklyHandle != null)
                    MelonCoroutines.Stop(_checkWeeklyHandle);

                _checkWeeklyHandle = MelonCoroutines.Start(CheckRollQuests(type));
            }
        }
    }

    private IEnumerator CheckRollQuests(String type)
    {
        if (PauseRoutines())
        {
            checkingDailies = false;
            checkingWeeklies = false;
            yield break;
        }

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

        if (PauseRoutines())
        {
            checkingDailies = false;
            checkingWeeklies = false;
            yield break;
        }

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
            Plugin.Logger.Msg("Resetting Dailies Reroll Counter");
            dailiesRerolled = 0;
    }

    private bool CanBeAutoCompleted(Quest quest)
    {
        QuestType questType = quest.questType;

        if (!Plugin.Settings.ResetReroll.Value || !Plugin.Settings.EnableAutoReroll.Value) return true;

        switch (questType)
        {
            //Quick Quests
            case QuestType.CompleteDailyQuests:
            case QuestType.PickUpCoins:
            case QuestType.PickUpGemstones:
            case QuestType.Boost:
                return true;

            //Chest Hunt Daily vs Weekly and if relevant quest is completed
            case QuestType.ChestHuntChests:
                {
                    var questTypeName = quest.GetIl2CppType().FullName;
                    if (ChestKeyQuest.isClaimed) return true; else return false;
                }

            //Kill With Rage Mode Daily vs Weekly
            case QuestType.KillEnemiesWithRageMode:
                {
                    var questTypeName = quest.GetIl2CppType().FullName;
                    if (questTypeName == "DailyQuest") return (dailiesRerolled > _hardRerollCap); else return false;
                }

            //Conditional Quests
            case QuestType.GetMaterials:
                if (Plugin.Settings.AutoRerollGetMaterials.Value)
                {
                    Plugin.Logger.Msg($"Quest '{quest.name}' - ({quest.questType}) rerolled per user configuration.");
                    return false;
                }
                else
                {
                    return ConditionalQuestCheck(quest);
                }
            case QuestType.KillEnemies:
            case QuestType.KillGiants:
                return ConditionalQuestCheck(quest);

            //Medium Quests
            case QuestType.BonusStage:
            case QuestType.BonusStageSections:
            case QuestType.CompleteAscendingHeights:
                if (dailiesRerolled > _softRerollCap)
                {
                    Plugin.Logger.Msg($"Quest '{quest.name}' - ({quest.questType}) kept because soft cap ({_softRerollCap}) reached. Total Rerolls: {dailiesRerolled}");
                    return true;
                }
                else
                {
                    return false;
                }
            //Long Quests
            case QuestType.HitRandomBoxes:
                if (!Plugin.Settings.AutoRerollHitRandomBox.Value || (dailiesRerolled > _hardRerollCap))
                {
                    Plugin.Logger.Msg($"Quest '{quest.name}' - ({quest.questType}) kept because hard cap ({_hardRerollCap}) reached. Total Rerolls: {dailiesRerolled}");
                    return true;
                }
                else
                {
                    return false;
                }
            case QuestType.HitRandomSilverBoxes:
                if (!Plugin.Settings.AutoRerollHitSilverBox.Value || (dailiesRerolled > _hardRerollCap))
                {
                    Plugin.Logger.Msg($"Quest '{quest.name}' - ({quest.questType}) kept because hard cap ({_hardRerollCap}) reached. Total Rerolls:  {dailiesRerolled}");
                    return true;
                }
                else
                {
                    return false;
                }
            case QuestType.UseRageMode:
                if (dailiesRerolled > _hardRerollCap)
                {
                    Plugin.Logger.Msg($"Quest '{quest.name}' - ({quest.questType}) kept because hard cap ({_hardRerollCap}) reached. Total Rerolls:  {dailiesRerolled}");
                    return true;
                }
                else
                {
                    return false;
                }

            //Manual Quest
            case QuestType.CraftTemporaryItems:
            case QuestType.GoThroughPortals:
            case QuestType.WindDashKills:
                return false;

            default:
                Plugin.Logger.Warning($"Unhandled QuestType: {questType} - {quest.name}");   
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
                if (dailiesRerolled > _softRerollCap)
                {
                    Plugin.Logger.Msg($"Quest '{quest.name}' - ({quest.questType}) kept because soft cap ({_softRerollCap}) reached.");
                    return true;
                }
                else
                {
                    return false;
                }   
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

    private void RerollQuest(Quest quest, string type)
    {
        MelonCoroutines.Start(RerollQuestWithDelay(quest, type));
    }

    private IEnumerator RerollQuestWithDelay(Quest quest, String type)
    {
        if (PauseRoutines()) yield break;

        if (quest == null)  yield break;

        if (type == "Daily")
        {
            var dq = new DailyQuest(quest.Pointer);

            Plugin.Logger.Msg($"Rerolling Daily Quest: {dq.questType} - {dq.name} - Goal: {dq.questCurrentGoal}/{dq.questGoal}. Total Rerolls: {dailiesRerolled}");

            _dailyQuestReroll.PrepareReroll(dq);
            yield return new WaitForSeconds(0.01f);

            try
            {
                _dailyQuestReroll.RewardForShowing();
                dailiesRerolled++;
            }
            catch (NullReferenceException)
            {
                Plugin.Logger.Warning("Skipped DailyQuestReroll.RewardForShowing due to null inside");
            }
        }
        else if (type == "Weekly")
        {
            var wq = new WeeklyQuest(quest.Pointer);

            _weeklyQuestReroll.PrepareReroll(wq);
            yield return new WaitForSeconds(0.01f);

            try
            {
                _weeklyQuestReroll.RewardForShowing();
            }
            catch (NullReferenceException)
            {
                Plugin.Logger.Warning("Skipped WeeklyQuestReroll.RewardForShowing due to null inside");
            }
        }
    }

    [HarmonyPatch(typeof(MapController), "ChangeMap")]
    public class Patch_MapControllerChangeMap
    {
        static void Postfix(MapController __instance)
        {
            if (__instance == null) return;
            Enhanced_Quests.Instance.DelayedReroll("Daily");
        }
    }

    [HarmonyPatch(typeof(DailyQuestsManager), "RegenerateDailys")]
    public class Patch_DailyQuestsManagerRegenerateDailys
    {
        static void Postfix(DailyQuestsManager __instance)
        {
            if (__instance == null) return;

            if (Enhanced_Quests.Instance.PauseRoutines())
                Enhanced_Quests.Instance.SetTriggerRegen("Daily");
            else
                Enhanced_Quests.Instance.DelayedReroll("Daily");
        }
    }
    [HarmonyPatch(typeof(WeeklyQuestsManager), "RegenerateWeeklies")]
    public class Patch_WeeklyQuestsManagerRegenerateWeeklies
    {
        static void Postfix(WeeklyQuestsManager __instance)
        {
            if (__instance == null) return;
            if (Enhanced_Quests.Instance.PauseRoutines())
                Enhanced_Quests.Instance.SetTriggerRegen("Weekly");
            else
                Enhanced_Quests.Instance.DelayedReroll("Weekly");
        }
    }

    [HarmonyPatch(typeof(DailyQuestsManager), "AddQuests")]
    public class Patch_DailyQuestsManagerAddQuests
    {
        static void Postfix(DailyQuestsManager __instance)
        {
            if (__instance == null) return;
            Enhanced_Quests.Instance.DelayedReroll("Daily");
        }
    }

    [HarmonyPatch(typeof(WeeklyQuestsManager), "AddQuests")]
    public class Patch_WeeklyQuestsManagerAddQuests
    {
        static void Postfix(WeeklyQuestsManager __instance)
        {
            if (__instance == null) return;
            Enhanced_Quests.Instance.DelayedReroll("Weekly");
        }
    }

    [HarmonyPatch(typeof(DailyQuest), "Claim")]
    public static class Patch_DailyQuestClaim
    {
        static void Postfix(Quest __instance)
        {
            if (__instance == null) return;
            if (Enhanced_Quests.Instance.PauseRoutines())
                Enhanced_Quests.Instance.SetTriggerRegen("Regen");
            else
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
            if (Enhanced_Quests.Instance.PauseRoutines())
                Enhanced_Quests.Instance.SetTriggerRegen("Regen");
            else
                Enhanced_Quests.Instance?.Invoke(
                    nameof(Enhanced_Quests.CheckToRegenerate), 2f);
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

    [HarmonyPatch(typeof(ChestHuntKey), "OnObjectSpawn")]
    public class Patch_ChestHuntKeyOnObjectSpawned
    {
        static void Postfix(ChestHuntKey __instance)
        {
            if (__instance == null) return;
            Enhanced_Quests.Instance?.SetKeySpawn();
        }
    }

    public void SetKeySpawn()
    {
        chestKeySpawned = true;
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
