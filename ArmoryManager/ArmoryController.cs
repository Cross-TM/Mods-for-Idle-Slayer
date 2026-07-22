using System.Collections;
using ArmoryManager.Core;
using ArmoryManager.Game;
using IdleSlayerMods.Common.Extensions;
using Il2Cpp;
using Il2CppInterop.Runtime.Attributes;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

namespace ArmoryManager;

public sealed class ArmoryController : MonoBehaviour
{
    private const float DebounceSeconds = 0.4f;
    private static readonly Divinity DuplicateNextPick = Divinities.list.DuplicateNextPick;
    private static readonly Divinity QuadrupleNextPick = Divinities.list.x2x2;
    private static readonly AscensionSkill ChestInAChest = AscensionSkills.list.ChestInAChest;

    public static ArmoryController Instance { get; private set; }

    private WeaponsManager _weaponsManager;
    private PlayerInventory _playerInventory;
    private ProtectedItemStore _protectedItems;
    private ArmorySnapshotFactory _snapshots;
    private ArmoryBreakService _breakService;
    private ArmoryVisuals _visuals;
    private readonly ArmoryScanState _scanState = new();
    private Coroutine _worker;

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Plugin.Logger.Warning("Duplicate Armory Manager controller was removed.");
            enabled = false;
            UnityEngine.Object.Destroy(this);
            return;
        }

        Instance = this;
        _weaponsManager = WeaponsManager.instance;
        _playerInventory = PlayerInventory.instance;

        if (_weaponsManager == null || _playerInventory == null)
        {
            Plugin.Logger.Error("Armory Manager could not acquire the game managers.");
            enabled = false;
            return;
        }

        Plugin.Config.MigrateAndValidate((int)_weaponsManager.GetMaxSlots());
        _protectedItems = new ProtectedItemStore(
            Path.Combine(MelonEnvironment.UserDataDirectory, "ArmoryManagerFavorites.dat"),
            Plugin.Config.MigratedProtectedItemUids);
        _snapshots = new ArmorySnapshotFactory(_weaponsManager, _protectedItems);
        _breakService = new ArmoryBreakService(_weaponsManager, _playerInventory, DropsManager.instance);
        _visuals = new ArmoryVisuals(_weaponsManager, _protectedItems);
        ApplyRequirementPerLevel();
    }

    public void Start()
    {
        if (!enabled)
            return;

        RefreshVisuals();
        RequestScan("scene loaded");
    }

    public void OnDestroy()
    {
        if (_worker != null)
            MelonCoroutines.Stop(_worker);

        _visuals?.Restore();
        if (Instance == this)
            Instance = null;
    }

    internal void ConfigurationChanged()
    {
        Plugin.Config.MigrateAndValidate((int)_weaponsManager.GetMaxSlots());
        ApplyRequirementPerLevel();
        RefreshVisuals();
        RequestScan("configuration changed");
    }

    internal void RequestScan(string reason)
    {
        if (!enabled)
            return;

        // The game can emit OnEndDrag twice in the same frame. The pending scan evaluates the
        // latest inventory state, so a second request while its worker is alive is redundant.
        if (_worker != null)
        {
            Plugin.Logger.Msg($"Scan coalesced: {reason}.");
            return;
        }

        ApplyRequirementPerLevel();
        Plugin.Logger.Msg($"Scan requested: {reason}.");
        _scanState.Request();
        _worker ??= (Coroutine)MelonCoroutines.Start(ProcessPendingScans());
    }

    internal bool TryToggleProtection(ItemObject itemObject)
    {
        var item = itemObject?.currentItem;
        if (item == null)
            return false;

        if (!_weaponsManager.currentItemsByUid.ContainsKey(item.uid))
        {
            ArmoryUi.Notify("Key Armory items are always protected.", true);
            return true;
        }

        var isProtected = _protectedItems.Toggle(item.uid);
        RefreshVisuals();
        RequestScan("item favorite changed");
        ArmoryUi.Notify(
            $"{item.item?.localizedName ?? item.item?.name ?? "Item"} is now " +
            (isProtected ? "a gold ★ favorite and can never be broken." :
                "not a favorite."),
            true);
        return true;
    }

    internal void DragStarted() => _scanState.StartDragging();

    internal void DragEnded()
    {
        _scanState.EndDragging();
        RequestScan("item drag ended");
    }

    internal void SuspendForAscension()
    {
        _scanState.SuspendUntil(Time.unscaledTime + 5f);
        _scanState.CancelPendingRequest();
    }

    internal void RefreshVisuals() => _visuals?.Refresh(GetDesiredFreeSlots());

    [HideFromIl2Cpp]
    private IEnumerator ProcessPendingScans()
    {
        try
        {
            while (_scanState.HasPendingRequest)
            {
                _scanState.ConsumeRequest();
                yield return new WaitForSecondsRealtime(DebounceSeconds);

                while (Plugin.Config.AutomationEnabled.Value && !IsSafeToMutate())
                    yield return new WaitForSecondsRealtime(DebounceSeconds);

                if (!Plugin.Config.AutomationEnabled.Value)
                {
                    RefreshVisuals();
                    try
                    {
                        var preview = Evaluate();
                        LogGridMeasurement("read-only preview; master switch OFF", preview);
                        Plugin.Logger.Msg(
                            "Armory cleanup preview only: the master switch is OFF, so no item can be broken.");
                    }
                    catch (Exception exception)
                    {
                        Plugin.Logger.Error($"Read-only Armory grid preview failed: {exception}");
                    }

                    continue;
                }

                try
                {
                    ExecuteEvaluation(Evaluate());
                }
                catch (Exception exception)
                {
                    Plugin.Logger.Error($"Armory scan failed: {exception}");
                }
            }
        }
        finally
        {
            _worker = null;
        }
    }

    [HideFromIl2Cpp]
    private void ExecuteEvaluation(ArmoryEvaluation evaluation)
    {
        LogGridMeasurement("before cleanup", evaluation);
        if (!evaluation.IsGridReliable)
        {
            var message = $"Cleanup skipped for safety: {evaluation.GridFailureReason}.";
            Plugin.Logger.Error(message);
            ArmoryUi.Notify($"<color=#ff5b5b>{message}</color>");
            return;
        }

        var result = _breakService.Execute(evaluation.Candidates, IsStillCandidate);
        var after = Evaluate();
        LogGridMeasurement("after cleanup", after);
        RefreshVisuals();

        if (result.BrokenCount > 0)
        {
            ArmoryUi.Notify(
                $"Broke {result.BrokenCount} Armory item(s)<br>" +
                $"+{result.DivinityPoints:N0}<sprite name=\"dp\"> | " +
                $"+{result.Scrap:N0}<sprite name=\"drop_scrap\">");
        }

        if (after.CurrentFreeSlots < after.DesiredFreeSlots)
        {
            var minimumIsBlocking = after.CandidatesBlockedByMinimum > 0
                                    || after.CurrentItemCount <= after.MinimumItemsToKeep;
            var explanation = minimumIsBlocking
                ? $"The first {after.MinimumItemsToKeep} physical storage slots are absolutely protected, " +
                  "counting from the top-left, left-to-right."
                : "No eligible low-level or worse duplicate items were found.";
            var message = $"Armory needs {after.DesiredFreeSlots} free slots, but only " +
                          $"{after.CurrentFreeSlots} are currently free. {explanation}";
            Plugin.Logger.Warning(message);
            ArmoryUi.Notify($"<color=#ff9a42>{message}</color>");
            LogScanDetails();
        }
    }

    private bool IsStillCandidate(uint uid)
    {
        if (!Plugin.Config.AutomationEnabled.Value || !IsSafeToMutate() || _protectedItems.Contains(uid))
            return false;

        if (!_weaponsManager.currentItemsByUid.TryGetValue(uid, out var item) || item == null)
            return false;

        return !item.IsEquipped()
               && _weaponsManager.activeLoadoutUids?.Contains(uid) != true
               && _weaponsManager.tempEquippedUids?.Contains(uid) != true;
    }

    [HideFromIl2Cpp]
    private ArmoryEvaluation Evaluate()
    {
        var snapshot = _snapshots.Create();
        return ArmoryEvaluator.Evaluate(
            snapshot.Items,
            new ArmoryRules(
            true,
            Plugin.Config.LowLevelMaximum.Value,
            true,
            GetDesiredFreeSlots(),
            Plugin.Config.GetEnabledGearTypes(),
            Plugin.Config.MinimumItemsToKeep.Value),
            snapshot.Grid);
    }

    private int GetDesiredFreeSlots()
    {
        var max = Math.Max(0, (int)_weaponsManager.GetMaxSlots() - 1);
        var desired = !ChestInAChest.unlocked ? 1
            : QuadrupleNextPick.unlocked ? 4
            : DuplicateNextPick.unlocked ? 2
            : 1;
        return Math.Clamp(desired, 0, max);
    }

    private bool IsSafeToMutate() => enabled
                                            && _scanState.CanMutate(GameState.IsRunner(), Time.unscaledTime)
                                            && _weaponsManager != null
                                            && _playerInventory != null;

    private void LogScanDetails()
    {
        var enabledTypes = Plugin.Config.GetEnabledGearTypes();
        var maximumLevel = Plugin.Config.LowLevelMaximum.Value;
        var summaries = _snapshots.Create().Items
            .Where(item => !item.IsKey)
            .GroupBy(item => string.IsNullOrWhiteSpace(item.RawGearType) ? "Unknown" : item.RawGearType)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var normalized = GearTypes.Normalize(group.Key);
                var allowed = normalized != null && enabledTypes.Contains(normalized);
                var levels = group.Select(item => item.Level).ToArray();
                var lowAndRemovable = group.Count(item => item.IsRemovable && item.Level <= maximumLevel);
                var favorites = group.Count(item => item.IsProtected);
                var equipped = group.Count(item => item.IsEquipped);
                return $"{group.Key}: count={group.Count()}, levels={levels.Min()}-{levels.Max()}, " +
                       $"removable<={maximumLevel}={lowAndRemovable}, favorites={favorites}, " +
                       $"equipped={equipped}, allowed={allowed}";
            });

        Plugin.Logger.Warning("Armory scan details | " + string.Join("; ", summaries));
    }

    private static void LogGridMeasurement(string phase, ArmoryEvaluation evaluation)
    {
        if (!evaluation.IsGridReliable)
        {
            Plugin.Logger.Error(
                $"Armory grid NOT verified ({phase}) | reason={evaluation.GridFailureReason}; cleanup=blocked");
            return;
        }

        var protectedEnd = Math.Min(evaluation.MinimumItemsToKeep, evaluation.StorageCapacity);
        var protectedSlots = protectedEnd == 0 ? "none" : $"1-{protectedEnd}";
        Plugin.Logger.Msg(
            $"Armory grid verified ({phase}) | capacity={evaluation.StorageCapacity}, " +
            $"occupied={evaluation.CurrentItemCount}, free={evaluation.CurrentFreeSlots}, " +
            $"target-free={evaluation.DesiredFreeSlots}, protected-slots={protectedSlots}, " +
            $"eligible={evaluation.Candidates.Count}, projected-free={evaluation.ProjectedFreeSlots}.");
    }

    private void ApplyRequirementPerLevel()
    {
        if (_weaponsManager.currentItemsByUid != null)
        {
            foreach (var item in _weaponsManager.currentItemsByUid.Values)
            {
                if (item?.item != null)
                    item.item.levelRequirementIncreasePerLevel = Plugin.Config.RequirementPerLevel.Value;
            }
        }

        if (_weaponsManager.currentKeyItemsByUid == null)
            return;

        foreach (var item in _weaponsManager.currentKeyItemsByUid.Values)
        {
            if (item?.item != null)
                item.item.levelRequirementIncreasePerLevel = Plugin.Config.RequirementPerLevel.Value;
        }
    }
}
