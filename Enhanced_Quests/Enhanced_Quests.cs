using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.Attributes;
using MelonLoader;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Enhanced_Quests;

public sealed class Enhanced_Quests : MonoBehaviour
{
    private const int MaxRerollsPerSlot = 100;

    private enum QuestDisposition
    {
        Accept,
        Reroll,
        Defer
    }

    public static Enhanced_Quests Instance { get; private set; }

    private QuestManager _questManager;
    private DailyQuestsManager _dailyQuestsManager;
    private WeeklyQuestsManager _weeklyQuestsManager;
    private DailyQuestReroll _dailyQuestReroll;
    private WeeklyQuestReroll _weeklyQuestReroll;
    private PermanentCraftableItem _dailyQuests;
    private PermanentCraftableItem _weeklyQuests;

    private object _scheduledProcessingHandle;
    private float _nextSettingsPoll;
    private bool _initialProcessingComplete;
    private bool _processingRequested;
    private bool _isProcessingQuests;
    private bool _claimInProgress;
    private bool _extraDailyAcceptanceInProgress;
    private bool _extraWeeklyAcceptanceInProgress;
    private bool _dailyRerollInProgress;
    private bool _weeklyRerollInProgress;
    private bool _whetstoneMissingWarningShown;
    private bool _warnedNoDailyTypes;
    private bool _warnedNoWeeklyTypes;
    private bool _dailyRegenerationAttempted;
    private bool _weeklyRegenerationAttempted;
    private string _lastDailySummarySignature;
    private string _lastWeeklySummarySignature;
    private string _lastObservedQuestSignature;
    private string _dailyUnavailablePoolSignature;
    private string _weeklyUnavailablePoolSignature;

    private readonly HashSet<string> _unknownQuestWarnings = new();

    public void Awake()
    {
        Instance = this;
        RefreshDependencies();
    }

    public void Start()
    {
        CheckToAllowQuestReroll();
        RequestProcessing(1f);
    }

    public void Update()
    {
        if (Time.realtimeSinceStartup < _nextSettingsPoll)
            return;

        _nextSettingsPoll = Time.realtimeSinceStartup + 1f;
        bool settingsChanged = Plugin.Settings?.ReloadIfChanged() == true;
        if (settingsChanged)
        {
            Plugin.Logger.Msg("Quest settings reloaded");
            _dailyUnavailablePoolSignature = null;
            _weeklyUnavailablePoolSignature = null;
        }

        CheckToAllowQuestReroll();

        string questSignature = GetActiveQuestSignature();
        bool questSetChanged = _lastObservedQuestSignature != null && _lastObservedQuestSignature != questSignature;
        _lastObservedQuestSignature = questSignature;
        bool hasClaimableQuest = Plugin.Settings?.AutoClaimQuests.Value == true &&
            SnapshotClaimCandidates().Any(SafeCanBeClaimed);

        if (settingsChanged || questSetChanged || hasClaimableQuest || !_initialProcessingComplete)
            RequestProcessing(0.1f);
    }

    public void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void RequestQuestProcessing(float delay = 0.35f)
    {
        Instance?.RequestProcessing(delay);
    }

    private void RequestProcessing(float delay)
    {
        if (Plugin.Settings == null || !Plugin.Settings.Enabled.Value)
            return;

        _processingRequested = true;
        if (_isProcessingQuests || _scheduledProcessingHandle != null)
            return;

        _scheduledProcessingHandle = MelonCoroutines.Start(DelayedProcessing(Math.Max(0.05f, delay)));
    }

    [HideFromIl2Cpp]
    private IEnumerator DelayedProcessing(float delay)
    {
        yield return new WaitForSeconds(delay);
        _scheduledProcessingHandle = null;

        if (!_processingRequested || _isProcessingQuests)
            yield break;

        _processingRequested = false;
        yield return ProcessQuests();
    }

    [HideFromIl2Cpp]
    private IEnumerator ProcessQuests()
    {
        if (_isProcessingQuests)
            yield break;

        _isProcessingQuests = true;
        try
        {
            if (Plugin.Settings == null || !Plugin.Settings.Enabled.Value)
                yield break;

            RefreshDependencies();
            if (!TryGetStableRunnerMap(out BaseMap currentMap))
                yield break;

            if (!TryRefreshQuestList())
                yield break;

            if (Plugin.Settings.AutoClaimQuests.Value)
                yield return ClaimAllAvailableQuests();

            yield return RegenerateEmptyQuestSets();

            yield return AcceptExtraDailyQuests();
            yield return AcceptExtraWeeklyQuest();

            if (!TryRefreshQuestList() || !TryGetStableRunnerMap(out currentMap))
                yield break;

            IntPtr mapPointer = currentMap.Pointer;

            if (Plugin.Settings.AutoRerollDailyQuests.Value)
                yield return ProcessQuestGroup(true, mapPointer);

            if (!TryGetStableRunnerMap(out currentMap) || currentMap.Pointer != mapPointer)
                yield break;

            if (Plugin.Settings.AutoRerollWeeklyQuests.Value)
                yield return ProcessQuestGroup(false, mapPointer);

            yield return CraftWhetstonesForTemporaryItemQuests();

            _initialProcessingComplete = true;
        }
        finally
        {
            if (_dailyRerollInProgress && _dailyQuestReroll != null)
                _dailyQuestReroll.rerollEnabled = true;
            if (_weeklyRerollInProgress && _weeklyQuestReroll != null)
                _weeklyQuestReroll.rerollEnabled = true;

            _claimInProgress = false;
            _extraDailyAcceptanceInProgress = false;
            _extraWeeklyAcceptanceInProgress = false;
            _dailyRerollInProgress = false;
            _weeklyRerollInProgress = false;
            _isProcessingQuests = false;

            if (_processingRequested && _scheduledProcessingHandle == null)
                RequestProcessing(0.35f);
        }
    }

    private void RefreshDependencies()
    {
        _questManager = QuestManager.instance;
        _dailyQuestsManager = DailyQuestsManager.instance;
        _weeklyQuestsManager = WeeklyQuestsManager.instance;
        _dailyQuestReroll = DailyQuestReroll.instance;
        _weeklyQuestReroll = WeeklyQuestReroll.instance;
        CraftableItems craftables = CraftableItems.list;
        if (craftables != null)
        {
            _dailyQuests = craftables.DailyQuests;
            _weeklyQuests = craftables.WeeklyQuests;
        }
    }

    private bool TryRefreshQuestList()
    {
        try
        {
            // Quest state is read directly from PlayerInventory below. Calling
            // QuestsList.RefreshList() while polling resets the game's quest-button
            // highlight, which made an already handled completion flash forever.
            // The game refreshes its own UI after claims, rerolls and regeneration.
            PlayerInventory inventory = PlayerInventory.instance;
            return inventory != null &&
                (inventory.allQuests != null || inventory.dailyQuests != null || inventory.weeklyQuests != null);
        }
        catch (Exception ex)
        {
            Plugin.Logger.Warning($"Unable to refresh quest list: {ex.Message}");
            return false;
        }
    }

    private bool TryGetStableRunnerMap(out BaseMap map)
    {
        map = null;
        try
        {
            if (!GameState.IsRunner())
                return false;

            MapController controller = MapController.instance;
            if (controller == null || controller.changingMap || controller.selectedMap == null)
                return false;

            BaseMap selectedMap = controller.selectedMap;
            map = selectedMap;
            return true;
        }
        catch
        {
            return false;
        }
    }

    [HideFromIl2Cpp]
    private IEnumerator ClaimAllAvailableQuests()
    {
        if (_claimInProgress)
            yield break;

        HashSet<IntPtr> attempted = new();
        for (int claims = 0; claims < 20; claims++)
        {
            if (!TryRefreshQuestList())
                yield break;

            Quest claimable = SnapshotClaimCandidates()
                .FirstOrDefault(q => !attempted.Contains(q.Pointer) && SafeCanBeClaimed(q));
            if (claimable == null)
                yield break;

            attempted.Add(claimable.Pointer);
            string kind = QuestKind(claimable);
            _claimInProgress = true;
            try
            {
                claimable.Claim();
                Notify($"Claimed {kind} quest: {DisplayQuest(claimable)}", true);
            }
            catch (Exception ex)
            {
                Plugin.Logger.Warning($"Failed to claim {kind} quest: {ex.Message}");
            }

            yield return new WaitForSeconds(0.5f);
            TryRefreshQuestList();
            _claimInProgress = false;
        }
    }

    [HideFromIl2Cpp]
    private IEnumerator RegenerateEmptyQuestSets()
    {
        if (!TryRefreshQuestList())
            yield break;

        int dailyCount = SnapshotTimedQuests(true).Count;
        int weeklyCount = SnapshotTimedQuests(false).Count;

        if (dailyCount > 0)
            _dailyRegenerationAttempted = false;
        else if (!_dailyRegenerationAttempted &&
                 _dailyQuestsManager != null && _dailyQuests != null && _dailyQuests.IsActive())
        {
            _dailyRegenerationAttempted = true;
            if (TryRegenerateDailyQuests())
            {
                yield return new WaitForSeconds(0.75f);
                TryRefreshQuestList();
            }
        }

        if (weeklyCount > 0)
            _weeklyRegenerationAttempted = false;
        else if (!_weeklyRegenerationAttempted &&
                 _weeklyQuestsManager != null && _weeklyQuests != null && _weeklyQuests.IsActive())
        {
            _weeklyRegenerationAttempted = true;
            if (TryRegenerateWeeklyQuests())
            {
                yield return new WaitForSeconds(0.75f);
                TryRefreshQuestList();
            }
        }
    }

    private bool TryRegenerateDailyQuests()
    {
        try
        {
            _dailyQuestsManager.RegenerateDailys();
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Logger.Warning($"Daily quest regeneration failed: {ex.Message}");
            return false;
        }
    }

    private bool TryRegenerateWeeklyQuests()
    {
        try
        {
            _weeklyQuestsManager.RegenerateWeeklies();
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Logger.Warning($"Weekly quest regeneration failed: {ex.Message}");
            return false;
        }
    }

    [HideFromIl2Cpp]
    private IEnumerator AcceptExtraDailyQuests()
    {
        ExtraDailyQuests extra = ExtraDailyQuests.instance;
        if (_extraDailyAcceptanceInProgress || extra == null || extra.granted || extra.extraQuestsToAdd <= 0 ||
            _dailyQuestsManager == null || _dailyQuests == null || !_dailyQuests.IsActive())
            yield break;

        _extraDailyAcceptanceInProgress = true;
        int amount = extra.extraQuestsToAdd;
        bool accepted = true;
        try
        {
            extra.RewardForShowing();
        }
        catch (Exception ex)
        {
            Plugin.Logger.Warning($"Extra daily quests could not be accepted: {ex.Message}");
            accepted = false;
        }

        if (!accepted)
        {
            _extraDailyAcceptanceInProgress = false;
            yield break;
        }

        yield return new WaitForSeconds(0.6f);
        TryRefreshQuestList();
        if (extra.granted)
            Notify($"Accepted {amount} extra daily quests", true);
        _extraDailyAcceptanceInProgress = false;
    }

    [HideFromIl2Cpp]
    private IEnumerator AcceptExtraWeeklyQuest()
    {
        ExtraWeeklyQuests extra = ExtraWeeklyQuests.instance;
        if (_extraWeeklyAcceptanceInProgress || extra == null || extra.granted || extra.extraQuestsToAdd <= 0 ||
            _weeklyQuestsManager == null || _weeklyQuests == null || !_weeklyQuests.IsActive())
            yield break;

        _extraWeeklyAcceptanceInProgress = true;
        int amount = extra.extraQuestsToAdd;
        bool accepted = true;
        try
        {
            extra.RewardForShowing();
        }
        catch (Exception ex)
        {
            Plugin.Logger.Warning($"Extra weekly quest could not be accepted: {ex.Message}");
            accepted = false;
        }

        if (!accepted)
        {
            _extraWeeklyAcceptanceInProgress = false;
            yield break;
        }

        yield return new WaitForSeconds(0.6f);
        TryRefreshQuestList();
        if (extra.granted)
            Notify($"Accepted {amount} extra weekly quest", true);
        _extraWeeklyAcceptanceInProgress = false;
    }

    [HideFromIl2Cpp]
    private IEnumerator ProcessQuestGroup(bool daily, IntPtr expectedMapPointer)
    {
        if (daily && !Plugin.Settings.HasAllowedDailyTypes())
        {
            WarnNoSelectedTypes(true);
            yield break;
        }

        if (!daily && !Plugin.Settings.HasAllowedWeeklyTypes())
        {
            WarnNoSelectedTypes(false);
            yield break;
        }

        if (daily)
            _warnedNoDailyTypes = false;
        else
            _warnedNoWeeklyTypes = false;

        HashSet<IntPtr> exhaustedQuests = new();
        const int maximum = MaxRerollsPerSlot;

        while (true)
        {
            if (!TryGetExpectedMap(expectedMapPointer, out BaseMap currentMap) || !TryRefreshQuestList())
                yield break;

            Quest current = FindFirstUnwantedQuest(daily, currentMap, exhaustedQuests);
            if (current == null)
            {
                NotifyMatchingSummary(daily);
                yield break;
            }

            int slotRerolls = 0;
            while (slotRerolls < maximum)
            {
                if (!TryGetExpectedMap(expectedMapPointer, out currentMap) || !TryRefreshQuestList())
                    yield break;

                current = FindTimedQuestByPointer(daily, current.Pointer);
                if (current == null)
                    break;

                QuestDisposition disposition = EvaluateQuest(current, daily, currentMap, out string reason, out bool mapDependent);
                if (disposition == QuestDisposition.Accept)
                {
                    NotifyAcceptedReplacement(current, daily, mapDependent, reason);
                    break;
                }

                if (disposition == QuestDisposition.Defer)
                {
                    exhaustedQuests.Add(current.Pointer);
                    break;
                }

                string poolSignature = GetRerollPoolSignature(daily, currentMap);
                if (GetUnavailablePoolSignature(daily) == poolSignature)
                    yield break;

                if (!HasReachableAcceptableCandidate(daily, currentMap))
                {
                    SetUnavailablePoolSignature(daily, poolSignature);
                    Plugin.Logger.Warning($"No reachable allowed {(daily ? "daily" : "weekly")} quest remains in the current set; automatic rerolls are paused until the set, map, or preferences change.");
                    yield break;
                }

                SetUnavailablePoolSignature(daily, null);

                HashSet<IntPtr> before = SnapshotTimedQuests(daily).Select(q => q.Pointer).ToHashSet();
                string kind = daily ? "daily" : "weekly";
                string suffix = string.IsNullOrWhiteSpace(reason) ? string.Empty : $": {reason}";
                Notify($"Rerolling {kind} {current.questType} quest{suffix}", false);

                bool prepared = TryPrepareAutomaticReroll(current, daily);
                if (!prepared)
                {
                    exhaustedQuests.Add(current.Pointer);
                    break;
                }

                yield return new WaitForSeconds(0.1f);
                TryRefreshQuestList();

                if (FindTimedQuestByPointer(daily, current.Pointer) != null)
                    TryCompleteAutomaticReroll(current, daily);

                slotRerolls++;
                Quest replacement = null;
                for (int wait = 0; wait < 10 && replacement == null; wait++)
                {
                    yield return new WaitForSeconds(0.2f);
                    if (!TryGetExpectedMap(expectedMapPointer, out currentMap) || !TryRefreshQuestList())
                        yield break;

                    replacement = SnapshotTimedQuests(daily).FirstOrDefault(q => !before.Contains(q.Pointer));
                }

                FinishAutomaticReroll(daily);

                if (replacement == null)
                {
                    Plugin.Logger.Warning($"{kind} reroll did not produce a new active quest; this slot was stopped.");
                    exhaustedQuests.Add(current.Pointer);
                    break;
                }

                current = replacement;
                QuestDisposition replacementDisposition = EvaluateQuest(current, daily, currentMap,
                    out string replacementReason, out bool replacementMapDependent);
                if (replacementDisposition == QuestDisposition.Accept)
                {
                    NotifyAcceptedReplacement(current, daily, replacementMapDependent, replacementReason);
                    break;
                }

                if (replacementDisposition == QuestDisposition.Defer)
                {
                    exhaustedQuests.Add(current.Pointer);
                    break;
                }
            }

            if (slotRerolls >= maximum)
            {
                string kind = daily ? "daily" : "weekly";
                string message = $"Maximum rerolls reached for {kind} slot";
                Plugin.Logger.Warning(message);
                Notify(message, false);
                exhaustedQuests.Add(current.Pointer);
            }
        }
    }

    private bool HasReachableAcceptableCandidate(bool daily, BaseMap map)
    {
        PlayerInventory inventory = PlayerInventory.instance;
        if (inventory == null)
            return false;

        HashSet<QuestType> activeTypes = new();
        if (daily)
        {
            if (inventory.dailyQuests == null)
                return false;

            foreach (DailyQuest quest in inventory.dailyQuests)
            {
                if (quest != null && quest.active)
                    activeTypes.Add(quest.questType);
            }

            foreach (DailyQuest candidate in inventory.dailyQuests)
            {
                if (candidate == null || candidate.active || activeTypes.Contains(candidate.questType))
                    continue;

                try
                {
                    if (!candidate.CheckIfIsValid())
                        continue;
                }
                catch
                {
                    continue;
                }

                if (CandidateMatchesPreferences(candidate, true, map))
                    return true;
            }
        }
        else
        {
            if (inventory.weeklyQuests == null)
                return false;

            foreach (WeeklyQuest quest in inventory.weeklyQuests)
            {
                if (quest != null && quest.active)
                    activeTypes.Add(quest.questType);
            }

            foreach (WeeklyQuest candidate in inventory.weeklyQuests)
            {
                if (candidate != null && !candidate.active && !activeTypes.Contains(candidate.questType) &&
                    CandidateMatchesPreferences(candidate, false, map))
                    return true;
            }
        }

        return false;
    }

    private bool CandidateMatchesPreferences(Quest quest, bool daily, BaseMap map)
    {
        bool known = daily
            ? Plugin.Settings.TryGetDailyTypeAllowed(quest.questType, out bool allowed)
            : Plugin.Settings.TryGetWeeklyTypeAllowed(quest.questType, out allowed);

        if (!known)
        {
            WarnUnknownQuestType(daily, quest.questType);
            allowed = true;
        }

        if (!allowed)
            return false;

        if (!IsMapDependent(quest.questType))
            return true;

        return map?.currentStageEnemies != null && map.currentStageEnemies.Count > 0 &&
            CanProgressOnMap(quest, map, out _);
    }

    private string GetRerollPoolSignature(bool daily, BaseMap map)
    {
        PlayerInventory inventory = PlayerInventory.instance;
        List<string> questStates = new();
        if (daily && inventory?.dailyQuests != null)
        {
            foreach (DailyQuest quest in inventory.dailyQuests)
            {
                if (quest != null)
                    questStates.Add($"{quest.Pointer}:{quest.questType}:{quest.active}:{quest.isClaimed}");
            }
        }
        else if (!daily && inventory?.weeklyQuests != null)
        {
            foreach (WeeklyQuest quest in inventory.weeklyQuests)
            {
                if (quest != null)
                    questStates.Add($"{quest.Pointer}:{quest.questType}:{quest.active}:{quest.isClaimed}");
            }
        }

        questStates.Sort(StringComparer.Ordinal);
        List<string> enemyStates = new();
        if (map?.currentStageEnemies != null)
        {
            foreach (Enemy enemy in map.currentStageEnemies)
            {
                if (enemy != null)
                    enemyStates.Add(enemy.Pointer.ToString());
            }
        }

        enemyStates.Sort(StringComparer.Ordinal);
        string questState = string.Join("|", questStates);
        string enemyState = string.Join(",", enemyStates);
        return $"{map?.Pointer ?? IntPtr.Zero}:{enemyState}:{questState}";
    }

    private string GetUnavailablePoolSignature(bool daily) =>
        daily ? _dailyUnavailablePoolSignature : _weeklyUnavailablePoolSignature;

    private void SetUnavailablePoolSignature(bool daily, string signature)
    {
        if (daily)
            _dailyUnavailablePoolSignature = signature;
        else
            _weeklyUnavailablePoolSignature = signature;
    }

    [HideFromIl2Cpp]
    private Quest FindFirstUnwantedQuest(bool daily, BaseMap map, HashSet<IntPtr> exhausted)
    {
        foreach (Quest quest in SnapshotTimedQuests(daily))
        {
            if (exhausted.Contains(quest.Pointer))
                continue;

            if (EvaluateQuest(quest, daily, map, out _, out _) == QuestDisposition.Reroll)
                return quest;
        }

        return null;
    }

    private QuestDisposition EvaluateQuest(Quest quest, bool daily, BaseMap map, out string reason, out bool mapDependent)
    {
        reason = null;
        mapDependent = false;

        if (quest == null || SafeCanBeClaimed(quest) || SafeIsCompleted(quest))
            return QuestDisposition.Defer;

        bool known;
        bool allowed;
        if (daily)
            known = Plugin.Settings.TryGetDailyTypeAllowed(quest.questType, out allowed);
        else
            known = Plugin.Settings.TryGetWeeklyTypeAllowed(quest.questType, out allowed);

        if (!known)
        {
            WarnUnknownQuestType(daily, quest.questType);
            allowed = true;
        }

        mapDependent = IsMapDependent(quest.questType);
        if (mapDependent && (map == null || map.currentStageEnemies == null || map.currentStageEnemies.Count == 0))
            return QuestDisposition.Defer;

        if (!allowed)
        {
            reason = $"{quest.questType} is not selected";
            return QuestDisposition.Reroll;
        }

        if (!mapDependent)
            return QuestDisposition.Accept;

        bool canProgress = CanProgressOnMap(quest, map, out reason);
        return canProgress ? QuestDisposition.Accept : QuestDisposition.Reroll;
    }

    private bool CanProgressOnMap(Quest quest, BaseMap map, out string reason)
    {
        string mapName = DisplayMap(map);
        bool result;

        switch (quest.questType)
        {
            case QuestType.KillEnemies:
                result = SpecificOrGenericEnemyAvailable(quest, map, _ => true);
                break;
            case QuestType.KillEnemiesWithArrows:
                result = SpecificOrGenericEnemyAvailable(quest, map, enemy => !enemy.cantBeKilledWithArrows);
                break;
            case QuestType.KillEnemiesWithCharacter:
                if (quest.characterRequired != null && !quest.characterRequired.unlocked)
                {
                    reason = $"required character is not unlocked on {mapName}";
                    return false;
                }
                result = SpecificOrGenericEnemyAvailable(quest, map, _ => true);
                break;
            case QuestType.KillEnemiesOfType:
                result = quest.enemyType == null
                    ? map.currentStageEnemies.Count > 0
                    : AnyEnemy(map, enemy => enemy.type == quest.enemyType);
                break;
            case QuestType.KillGiants:
                result = quest.enemyToKill == null
                    ? AnyEnemy(map, enemy => enemy.isGiant)
                    : SpecificOrGenericEnemyAvailable(quest, map, enemy => enemy.isGiant);
                break;
            case QuestType.KillFlyers:
                result = AnyEnemy(map, enemy => enemy.isFlying);
                break;
            case QuestType.DoCriticals:
                result = AnyEnemy(map, enemy => !enemy.cantBeCritical);
                break;
            case QuestType.GetMaterials:
                result = AnyEnemy(map, enemy => enemy.drop != null);
                break;
            case QuestType.KillShinyEnemies:
                result = AnyEnemy(map, enemy => !enemy.cantBeShiny);
                break;
            case QuestType.KillEnemiesWithRageMode:
            case QuestType.WindDashKills:
                result = map.currentStageEnemies.Count > 0;
                break;
            default:
                reason = null;
                return true;
        }

        string target = TargetDescription(quest);
        reason = result
            ? $"{target} is available in {mapName}"
            : $"{target} is not available in {mapName}";
        return result;
    }

    [HideFromIl2Cpp]
    private bool SpecificOrGenericEnemyAvailable(Quest quest, BaseMap map, Func<Enemy, bool> additionalCondition)
    {
        if (quest.enemyToKill == null)
            return AnyEnemy(map, additionalCondition);

        foreach (Enemy enemy in map.currentStageEnemies)
        {
            if (enemy == null || !additionalCondition(enemy))
                continue;

            if (enemy == quest.enemyToKill)
                return true;

            try
            {
                if (_questManager != null && _questManager.EnemyFromSameStageOrBelow(quest.enemyToKill, enemy))
                    return true;
            }
            catch
            {
                // Exact object matching above remains valid if the evolution helper is unavailable.
            }
        }

        return false;
    }

    private static bool AnyEnemy(BaseMap map, Func<Enemy, bool> predicate)
    {
        foreach (Enemy enemy in map.currentStageEnemies)
        {
            if (enemy != null && predicate(enemy))
                return true;
        }

        return false;
    }

    private static bool IsMapDependent(QuestType type) => type is
        QuestType.KillEnemies or QuestType.KillEnemiesWithArrows or QuestType.KillFlyers or
        QuestType.DoCriticals or QuestType.KillGiants or QuestType.KillEnemiesWithRageMode or
        QuestType.GetMaterials or QuestType.KillEnemiesWithCharacter or QuestType.KillEnemiesOfType or
        QuestType.KillShinyEnemies or QuestType.WindDashKills;

    private bool TryPrepareAutomaticReroll(Quest quest, bool daily)
    {
        try
        {
            if (daily)
            {
                if (_dailyQuestReroll == null)
                    return false;
                _dailyRerollInProgress = true;
                _dailyQuestReroll.rerollEnabled = true;
                _dailyQuestReroll.PrepareReroll(new DailyQuest(quest.Pointer));
            }
            else
            {
                if (_weeklyQuestReroll == null)
                    return false;
                _weeklyRerollInProgress = true;
                _weeklyQuestReroll.rerollEnabled = true;
                _weeklyQuestReroll.PrepareReroll(new WeeklyQuest(quest.Pointer));
            }

            return true;
        }
        catch (Exception ex)
        {
            Plugin.Logger.Warning($"Unable to prepare {(daily ? "daily" : "weekly")} reroll: {ex.Message}");
            FinishAutomaticReroll(daily);
            return false;
        }
    }

    private void TryCompleteAutomaticReroll(Quest quest, bool daily)
    {
        if (quest == null || SafeCanBeClaimed(quest) || SafeIsCompleted(quest))
            return;

        try
        {
            if (daily && _dailyQuestReroll != null)
            {
                _dailyQuestReroll.rerollEnabled = true;
                _dailyQuestReroll.RewardForShowing();
            }
            else if (!daily && _weeklyQuestReroll != null)
            {
                _weeklyQuestReroll.rerollEnabled = true;
                _weeklyQuestReroll.RewardForShowing();
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.Warning($"Unable to complete {(daily ? "daily" : "weekly")} reroll: {ex.Message}");
        }
    }

    private void FinishAutomaticReroll(bool daily)
    {
        if (daily)
        {
            _dailyRerollInProgress = false;
            if (_dailyQuestReroll != null)
                _dailyQuestReroll.rerollEnabled = true;
        }
        else
        {
            _weeklyRerollInProgress = false;
            if (_weeklyQuestReroll != null)
                _weeklyQuestReroll.rerollEnabled = true;
        }
    }

    private bool TryGetExpectedMap(IntPtr expectedPointer, out BaseMap map) =>
        TryGetStableRunnerMap(out map) && map.Pointer == expectedPointer;

    [HideFromIl2Cpp]
    private IEnumerator CraftWhetstonesForTemporaryItemQuests()
    {
        int amountToCraft = SnapshotTimedQuests(null)
            .Where(quest => quest.questType == QuestType.CraftTemporaryItems &&
                            !SafeCanBeClaimed(quest) && !SafeIsCompleted(quest))
            .Select(quest => Math.Max(0, quest.questGoal - quest.questCurrentGoal))
            .DefaultIfEmpty(0)
            .Max();

        if (amountToCraft <= 0)
            yield break;

        PlayerInventory inventory = PlayerInventory.instance;
        TemporaryCraftableItem whetstone = FindWhetstone(inventory);
        if (whetstone == null)
        {
            if (!_whetstoneMissingWarningShown)
            {
                _whetstoneMissingWarningShown = true;
                Plugin.Logger.Warning("A Craft Temporary Items quest is active, but the Whetstone item was not found.");
            }
            yield break;
        }

        _whetstoneMissingWarningShown = false;
        int crafted = 0;
        for (; crafted < amountToCraft; crafted++)
        {
            double craftedBefore = inventory.temporaryItemsCrafted;
            try
            {
                if (whetstone.HowManyCanCraft() < 1d)
                    break;

                whetstone.Craft();
            }
            catch (Exception ex)
            {
                Plugin.Logger.Warning($"Unable to craft Whetstone: {ex.Message}");
                break;
            }

            if (inventory.temporaryItemsCrafted <= craftedBefore)
                break;

            yield return null;
        }

        if (crafted > 0)
            Notify($"Crafted {crafted} Whetstone{(crafted == 1 ? string.Empty : "s")} for temporary item quests", true);

        if (crafted < amountToCraft)
            Plugin.Logger.Warning($"Crafted {crafted} of {amountToCraft} required Whetstones; not enough materials were available.");
    }

    [HideFromIl2Cpp]
    private static TemporaryCraftableItem FindWhetstone(PlayerInventory inventory)
    {
        if (inventory?.temporaryCraftableItems == null)
            return null;

        foreach (TemporaryCraftableItem item in inventory.temporaryCraftableItems)
        {
            if (item != null &&
                (ContainsInvariant(item.name, "whetstone") || ContainsInvariant(item.localizedName, "whetstone")))
                return item;
        }

        return null;
    }

    private static bool ContainsInvariant(string value, string expected) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains(expected, StringComparison.OrdinalIgnoreCase);

    [HideFromIl2Cpp]
    private List<Quest> SnapshotTimedQuests(bool? daily)
    {
        List<Quest> result = new();
        PlayerInventory inventory = PlayerInventory.instance;
        if (inventory == null)
            return result;

        if (daily != false && inventory.dailyQuests != null)
        {
            foreach (DailyQuest quest in inventory.dailyQuests)
            {
                if (quest != null && quest.active && !quest.isClaimed)
                    result.Add(quest);
            }
        }

        if (daily != true && inventory.weeklyQuests != null)
        {
            foreach (WeeklyQuest quest in inventory.weeklyQuests)
            {
                if (quest != null && quest.active && !quest.isClaimed)
                    result.Add(quest);
            }
        }

        return result;
    }

    [HideFromIl2Cpp]
    private List<Quest> SnapshotClaimCandidates()
    {
        List<Quest> result = new();
        HashSet<IntPtr> pointers = new();
        PlayerInventory inventory = PlayerInventory.instance;

        if (inventory?.allQuests != null)
        {
            foreach (Quest quest in inventory.allQuests)
            {
                // The base Quest type does not expose the timed-quest `active`
                // flag. Completion checks below remain authoritative for normal
                // quests, while the dedicated timed collections add only active
                // Daily and Weekly quests.
                if (quest != null && !quest.isClaimed && pointers.Add(quest.Pointer))
                    result.Add(quest);
            }
        }

        // Keep the dedicated collections as a fallback in case a game version does
        // not expose timed quests through allQuests.
        foreach (Quest quest in SnapshotTimedQuests(null))
        {
            if (pointers.Add(quest.Pointer))
                result.Add(quest);
        }

        return result;
    }

    private Quest FindTimedQuestByPointer(bool daily, IntPtr pointer) =>
        SnapshotTimedQuests(daily).FirstOrDefault(q => q.Pointer == pointer);

    private string GetActiveQuestSignature() => string.Join("|", SnapshotClaimCandidates()
        .OrderBy(quest => quest.Pointer)
        .Select(quest => $"{quest.Pointer}:{quest.questType}:{quest.questGoal}"));

    private static bool IsDailyQuest(Quest quest) =>
        quest?.GetIl2CppType().FullName?.EndsWith("DailyQuest", StringComparison.Ordinal) == true;

    private static bool IsWeeklyQuest(Quest quest) =>
        quest?.GetIl2CppType().FullName?.EndsWith("WeeklyQuest", StringComparison.Ordinal) == true;

    private static string QuestKind(Quest quest)
    {
        if (IsDailyQuest(quest))
            return "daily";
        if (IsWeeklyQuest(quest))
            return "weekly";
        return "quest-line";
    }

    private static bool SafeCanBeClaimed(Quest quest)
    {
        try { return quest != null && quest.CanBeClaimed(); }
        catch { return false; }
    }

    private static bool SafeIsCompleted(Quest quest)
    {
        try { return quest != null && quest.IsCompleted(); }
        catch { return true; }
    }

    private void WarnNoSelectedTypes(bool daily)
    {
        if (daily && _warnedNoDailyTypes || !daily && _warnedNoWeeklyTypes)
            return;

        string kind = daily ? "daily" : "weekly";
        Plugin.Logger.Warning($"No allowed {kind} quest types are selected; {kind} rerolls are disabled.");
        if (daily)
            _warnedNoDailyTypes = true;
        else
            _warnedNoWeeklyTypes = true;
    }

    private void WarnUnknownQuestType(bool daily, QuestType type)
    {
        string key = $"{daily}:{type}";
        if (_unknownQuestWarnings.Add(key))
            Plugin.Logger.Warning($"Unknown {(daily ? "daily" : "weekly")} QuestType {type}; treating the type as allowed.");
    }

    private void NotifyAcceptedReplacement(Quest quest, bool daily, bool mapDependent, string reason)
    {
        if (mapDependent && !string.IsNullOrWhiteSpace(reason))
            Notify($"Accepted {quest.questType} quest: {reason}", true);
        else
            Notify($"{(daily ? "Daily" : "Weekly")} quest accepted: {DisplayQuest(quest)} ({quest.questGoal})", true);
    }

    private void NotifyMatchingSummary(bool daily)
    {
        string signature = string.Join("|", SnapshotTimedQuests(daily)
            .Select(q => $"{q.Pointer}:{q.questType}:{q.questGoal}"));

        if (daily)
        {
            if (_lastDailySummarySignature == signature)
                return;
            _lastDailySummarySignature = signature;
        }
        else
        {
            if (_lastWeeklySummarySignature == signature)
                return;
            _lastWeeklySummarySignature = signature;
        }

        Notify($"{(daily ? "Daily" : "Weekly")} quests match selected preferences", true);
    }

    private static string DisplayQuest(Quest quest)
    {
        if (quest == null)
            return "quest";
        if (!string.IsNullOrWhiteSpace(quest.localizedName))
            return quest.localizedName;
        return quest.questType.ToString();
    }

    private static string DisplayMap(BaseMap map)
    {
        if (map == null)
            return "current map";
        if (!string.IsNullOrWhiteSpace(map.localizedName))
            return map.localizedName;
        if (!string.IsNullOrWhiteSpace(map.name))
            return map.name;
        return "current map";
    }

    private static string TargetDescription(Quest quest)
    {
        if (quest.enemyToKill != null)
        {
            if (!string.IsNullOrWhiteSpace(quest.enemyToKill.localizedName))
                return quest.enemyToKill.localizedName;
            if (!string.IsNullOrWhiteSpace(quest.enemyToKill.name))
                return quest.enemyToKill.name;
        }

        if (quest.enemyType != null)
        {
            if (!string.IsNullOrWhiteSpace(quest.enemyType.localizedName))
                return quest.enemyType.localizedName;
            if (!string.IsNullOrWhiteSpace(quest.enemyType.name))
                return quest.enemyType.name;
        }

        return quest.questType switch
        {
            QuestType.KillGiants => "a suitable giant",
            QuestType.KillFlyers => "a flying enemy",
            QuestType.GetMaterials => "an enemy with material drops",
            QuestType.KillShinyEnemies => "a shiny-capable enemy",
            QuestType.DoCriticals => "a critical-hit-capable enemy",
            _ => "normal enemies"
        };
    }

    private static void Notify(string message, bool positive)
    {
        Plugin.Logger.Msg(message);
        Plugin.ModHelperInstance?.ShowNotification(message, positive);
    }

    public void CheckToAllowQuestReroll()
    {
        RefreshDependencies();
        if (_dailyQuestReroll != null && !_dailyQuestReroll.rerollEnabled)
            _dailyQuestReroll.rerollEnabled = true;

        if (_weeklyQuestReroll != null && !_weeklyQuestReroll.rerollEnabled)
            _weeklyQuestReroll.rerollEnabled = true;
    }

    [HarmonyPatch(typeof(DailyQuest), nameof(DailyQuest.Claim))]
    private static class DailyQuestClaimPatch
    {
        private static void Postfix() => RequestQuestProcessing(0.5f);
    }

    [HarmonyPatch(typeof(WeeklyQuest), nameof(WeeklyQuest.Claim))]
    private static class WeeklyQuestClaimPatch
    {
        private static void Postfix() => RequestQuestProcessing(0.5f);
    }

    [HarmonyPatch(typeof(DailyQuestsManager), nameof(DailyQuestsManager.RegenerateDailys))]
    private static class DailyRegenerationPatch
    {
        private static void Postfix() => RequestQuestProcessing(0.75f);
    }

    [HarmonyPatch(typeof(WeeklyQuestsManager), nameof(WeeklyQuestsManager.RegenerateWeeklies))]
    private static class WeeklyRegenerationPatch
    {
        private static void Postfix() => RequestQuestProcessing(0.75f);
    }

    [HarmonyPatch(typeof(DailyQuestsManager), nameof(DailyQuestsManager.AddQuests))]
    private static class DailyAddQuestsPatch
    {
        private static void Postfix() => RequestQuestProcessing(0.5f);
    }

    [HarmonyPatch(typeof(WeeklyQuestsManager), nameof(WeeklyQuestsManager.AddQuests))]
    private static class WeeklyAddQuestsPatch
    {
        private static void Postfix() => RequestQuestProcessing(0.5f);
    }

    [HarmonyPatch(typeof(ExtraDailyQuests), nameof(ExtraDailyQuests.RewardForShowing))]
    private static class ExtraDailyRewardPatch
    {
        private static void Postfix() => RequestQuestProcessing(0.5f);
    }

    [HarmonyPatch(typeof(ExtraWeeklyQuests), nameof(ExtraWeeklyQuests.RewardForShowing))]
    private static class ExtraWeeklyRewardPatch
    {
        private static void Postfix() => RequestQuestProcessing(0.5f);
    }

    [HarmonyPatch(typeof(DailyQuestReroll), nameof(DailyQuestReroll.RewardForShowing))]
    private static class DailyRerollRewardPatch
    {
        private static void Postfix()
        {
            Instance?.CheckToAllowQuestReroll();
            RequestQuestProcessing(0.5f);
        }

        private static Exception Finalizer(Exception __exception) =>
            __exception != null && Instance?._dailyRerollInProgress == true ? null : __exception;
    }

    [HarmonyPatch(typeof(WeeklyQuestReroll), nameof(WeeklyQuestReroll.RewardForShowing))]
    private static class WeeklyRerollRewardPatch
    {
        private static void Postfix()
        {
            Instance?.CheckToAllowQuestReroll();
            RequestQuestProcessing(0.5f);
        }

        private static Exception Finalizer(Exception __exception) =>
            __exception != null && Instance?._weeklyRerollInProgress == true ? null : __exception;
    }

    [HarmonyPatch(typeof(MapController), nameof(MapController.ChangeMap))]
    private static class MapChangePatch
    {
        private static void Postfix() => RequestQuestProcessing(1f);
    }

    [HarmonyPatch(typeof(MapController), nameof(MapController.SetCurrentStageEnemies))]
    private static class StageEnemiesReadyPatch
    {
        private static void Postfix() => RequestQuestProcessing(0.75f);
    }

}
