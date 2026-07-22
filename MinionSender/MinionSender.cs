using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace MinionManaging;

public class MinionSender : MonoBehaviour
{
    private const float PrestigeRetryDelaySeconds = 1f;
    private const float MinionScanIntervalSeconds = 1f;
    private const float DailyBonusRetryDelaySeconds = 5f;

    private const string WhiteKnightId = "u6ooepjz24aip6uhdwo3";
    private const string DarkWizardId = "49s9yt5zymcce045wc6s";
    private const string StoneGolemId = "8h046cp1ahye3ikg2k8y";
    private const string SkeletonId = "xnb273rqtfgvl8vwi6o8";
    private const string GooMutantId = "wrep9pscctn5fr96vhwc";
    private const string CyclopsId = "td2rabyncdgyesd4p0nb";
    private const string NinjaId = "6v8v9ylvhv5o63ksr5p3";

    private readonly Dictionary<string, ProcessingState> _states = new();
    private float _nextMinionScanAt;
    private float _nextDailyBonusAttemptAt;
    private bool _dailyBonusInProgress;
    private bool _dailyBonusHandledWhileAvailable;
    private bool _dailyBonusFailureLogged;
    private static int _automaticPrestigeRewardDepth;

    public static MinionSender Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (Plugin.Settings == null || Time.realtimeSinceStartup < _nextMinionScanAt)
            return;

        _nextMinionScanAt = Time.realtimeSinceStartup + MinionScanIntervalSeconds;
        ScanMinions();
        TryClaimDailyBonus();
    }

    private void ScanMinions()
    {
        foreach (var minion in Resources.FindObjectsOfTypeAll<Minion>())
        {
            if (!IsUsable(minion) || !minion.IsUnlocked())
                continue;

            if (minion.CanBeClaimed())
                continue;

            if (minion.IsStandingBy())
                continue;

            HandleActiveMission(minion);
        }
    }

    private void TryClaimDailyBonus()
    {
        if (!Plugin.Settings.AutoDailyBonusEnabled.Value || _dailyBonusInProgress ||
            Time.realtimeSinceStartup < _nextDailyBonusAttemptAt)
            return;

        var dailyBonus = DailyMinionBonus.instance;
        if (dailyBonus == null)
            return;

        try
        {
            dailyBonus.CheckDailyBonus();
            if (dailyBonus.button == null || !dailyBonus.button.interactable ||
                !dailyBonus.CanTriggerDailyBonus())
            {
                _dailyBonusHandledWhileAvailable = false;
                _dailyBonusFailureLogged = false;
                return;
            }

            if (_dailyBonusHandledWhileAvailable)
                return;

            _dailyBonusInProgress = true;
            _dailyBonusHandledWhileAvailable = true;
            var previousBonusTime = dailyBonus.dailyMinionBonusTime;
            dailyBonus.RewardForShowing();
            MelonCoroutines.Start(ProcessAfterDailyBonus(dailyBonus, previousBonusTime));
        }
        catch (Exception exception)
        {
            _dailyBonusInProgress = false;
            _dailyBonusHandledWhileAvailable = false;
            _nextDailyBonusAttemptAt = Time.realtimeSinceStartup + DailyBonusRetryDelaySeconds;
            LogDailyBonusFailure($"Failed to claim the daily minion bonus: {exception.Message}");
        }
    }

    private IEnumerator ProcessAfterDailyBonus(DailyMinionBonus dailyBonus, double previousBonusTime)
    {
        yield return null;
        yield return null;

        if (!IsCurrentInstance())
            yield break;

        _dailyBonusInProgress = false;

        try
        {
            if (dailyBonus == null)
                yield break;

            dailyBonus.CheckDailyBonus();
            var bonusTimeChanged = dailyBonus.dailyMinionBonusTime != previousBonusTime;
            var bonusStillAvailable = dailyBonus.button != null && dailyBonus.button.interactable &&
                                      dailyBonus.CanTriggerDailyBonus();
            if (!bonusTimeChanged && bonusStillAvailable)
            {
                _dailyBonusHandledWhileAvailable = false;
                _nextDailyBonusAttemptAt = Time.realtimeSinceStartup + DailyBonusRetryDelaySeconds;
                LogDailyBonusFailure("The daily minion bonus was not processed.");
                yield break;
            }

            _nextDailyBonusAttemptAt = 0f;
            _dailyBonusFailureLogged = false;
            ScanMinions();
        }
        catch (Exception exception)
        {
            _dailyBonusHandledWhileAvailable = false;
            _nextDailyBonusAttemptAt = Time.realtimeSinceStartup + DailyBonusRetryDelaySeconds;
            LogDailyBonusFailure($"Failed to verify the daily minion bonus: {exception.Message}");
        }
    }

    private void LogDailyBonusFailure(string message)
    {
        if (_dailyBonusFailureLogged)
            return;

        _dailyBonusFailureLogged = true;
        Plugin.Logger.Warning(message);
    }

    private void OnDestroy()
    {
        if (ReferenceEquals(Instance, this))
            Instance = null;

        _states.Clear();
        _dailyBonusInProgress = false;
    }

    private void TryClaim(Minion minion)
    {
        if (!IsUsable(minion))
            return;

        var minionId = GetMinionKey(minion);
        var state = GetState(minionId);
        if (state.ClaimInProgress || state.PrestigeInProgress || state.ResendInProgress)
            return;

        if (Time.realtimeSinceStartup < state.NextClaimAttemptAt)
            return;

        state.ClaimInProgress = true;

        try
        {
            minion.ClaimQuest(true);
            state.NextClaimAttemptAt = 0f;
            state.HoldAfterPrestige = false;
            state.AwaitingPostPrestigeResend = false;
            state.NotifiedTokens.Clear();
            state.LoggedTokens.Clear();
            MelonCoroutines.Start(ProcessAfterClaim(minion, minionId));
        }
        catch (Exception exception)
        {
            state.ClaimInProgress = false;
            state.NextClaimAttemptAt = Time.realtimeSinceStartup + PrestigeRetryDelaySeconds;
            LogOnce(state, "claim-failed", $"Failed to claim {GetDisplayName(minion)}: {exception.Message}");
        }
    }

    private IEnumerator ProcessAfterClaim(Minion minion, string minionId)
    {
        yield return null;

        if (!IsCurrentInstance() || !IsUsable(minion) || !_states.TryGetValue(minionId, out var state))
            yield break;

        if (minion.currentTaskTime != 0d)
        {
            state.ClaimInProgress = false;
            LogOnce(state, "claim-not-processed", $"Claim for {GetDisplayName(minion)} was not processed.");
            yield break;
        }

        var isMaxLevel = IsMaxLevel(minion);
        if (isMaxLevel)
            NotifyOnce(state, "max-level", $"{GetDisplayName(minion)} reached max level");

        var shouldPrestige = isMaxLevel && IsAutomaticPrestigeSelected(minion);
        if (isMaxLevel && !shouldPrestige)
        {
            NotifyOnce(state, "prestige-disabled",
                $"{GetDisplayName(minion)} was not prestiged because it is disabled in settings");
        }

        state.ClaimInProgress = false;

        if (shouldPrestige)
            TryPrestige(minion, state, false);
        else
            TryResend(minion, state, false);
    }

    private void HandleStandingBy(Minion minion)
    {
        if (!IsUsable(minion))
            return;

        var state = GetState(GetMinionKey(minion));
        if (state.ClaimInProgress || state.PrestigeInProgress || state.ResendInProgress)
            return;

        if (state.HoldAfterPrestige)
        {
            if (!Plugin.Settings.AutoResendAfterPrestige.Value)
                return;

            state.HoldAfterPrestige = false;
            state.AwaitingPostPrestigeResend = true;
        }

        if (IsMaxLevel(minion))
        {
            NotifyOnce(state, "max-level", $"{GetDisplayName(minion)} reached max level");

            if (IsAutomaticPrestigeSelected(minion))
            {
                TryPrestige(minion, state, false);
                return;
            }

            NotifyOnce(state, "prestige-disabled",
                $"{GetDisplayName(minion)} was not prestiged because it is disabled in settings");
        }

        TryResend(minion, state, state.AwaitingPostPrestigeResend);
    }

    private void HandleActiveMission(Minion minion)
    {
        if (!IsUsable(minion) || !Plugin.Settings.PrestigeActiveMissions.Value)
            return;

        if (minion.currentTaskTime <= 0d)
            return;

        var state = GetState(GetMinionKey(minion));
        if (state.ClaimInProgress || state.PrestigeInProgress || state.ResendInProgress)
            return;

        if (!IsMaxLevel(minion) || !IsAutomaticPrestigeSelected(minion))
            return;

        NotifyOnce(state, "max-level", $"{GetDisplayName(minion)} reached max level");
        TryPrestige(minion, state, true);
    }

    private void TryPrestige(Minion minion, ProcessingState state, bool allowActiveMission)
    {
        if (!IsUsable(minion) || state.ClaimInProgress || state.PrestigeInProgress || state.ResendInProgress)
            return;

        if (!IsAutomaticPrestigeSelected(minion) || !IsMaxLevel(minion))
            return;

        var hasActiveMission = minion.currentTaskTime != 0d;
        if (hasActiveMission && (!allowActiveMission || !Plugin.Settings.PrestigeActiveMissions.Value))
            return;

        if (Time.realtimeSinceStartup < state.NextPrestigeAttemptAt)
            return;

        var manager = DivinitiesManager.instance;
        if (manager == null)
        {
            state.NextPrestigeAttemptAt = Time.realtimeSinceStartup + PrestigeRetryDelaySeconds;
            LogOnce(state, "prestige-manager-unavailable",
                $"Cannot prestige {GetDisplayName(minion)} because DivinitiesManager is unavailable.");
            return;
        }

        state.PrestigeInProgress = true;
        var displayName = GetDisplayName(minion);
        NotifyOnce(state, "prestiging", $"Prestiging {displayName}");

        try
        {
            _automaticPrestigeRewardDepth++;
            try
            {
                manager.PrestigeMinion(minion);
            }
            finally
            {
                _automaticPrestigeRewardDepth--;
            }

            MelonCoroutines.Start(VerifyPrestige(minion, GetMinionKey(minion), displayName));
        }
        catch (Exception exception)
        {
            state.PrestigeInProgress = false;
            state.NextPrestigeAttemptAt = Time.realtimeSinceStartup + PrestigeRetryDelaySeconds;
            LogOnce(state, "prestige-failed", $"Failed to prestige {displayName}: {exception.Message}");
        }
    }

    private IEnumerator VerifyPrestige(Minion minion, string minionId, string displayName)
    {
        yield return null;

        if (!IsCurrentInstance() || !IsUsable(minion) || !_states.TryGetValue(minionId, out var state))
            yield break;

        var prestigeSucceeded = minion.level == 1 && minion.IsStandingBy();
        state.PrestigeInProgress = false;

        if (!prestigeSucceeded)
        {
            state.NextPrestigeAttemptAt = Time.realtimeSinceStartup + PrestigeRetryDelaySeconds;
            LogOnce(state, "prestige-not-processed", $"Prestige for {displayName} was not processed.");
            yield break;
        }

        state.NextPrestigeAttemptAt = 0f;
        state.LoggedTokens.Clear();
        NotifyOnce(state, "prestige-success", $"{displayName} prestiged successfully");

        if (!Plugin.Settings.AutoResendAfterPrestige.Value)
        {
            state.HoldAfterPrestige = true;
            state.AwaitingPostPrestigeResend = false;
            yield break;
        }

        state.AwaitingPostPrestigeResend = true;
        TryResend(minion, state, true);
    }

    private bool TryResend(Minion minion, ProcessingState state, bool afterPrestige)
    {
        if (!IsUsable(minion) || state.ClaimInProgress || state.PrestigeInProgress || state.ResendInProgress)
            return false;

        if (afterPrestige && !Plugin.Settings.AutoResendAfterPrestige.Value)
        {
            state.HoldAfterPrestige = true;
            state.AwaitingPostPrestigeResend = false;
            return false;
        }

        if (Time.realtimeSinceStartup < state.NextResendAttemptAt)
            return false;

        state.ResendInProgress = true;

        try
        {
            if (!minion.IsStandingBy() || !minion.IsUnlocked())
                return false;

            if (minion.GetCost() > SlayerPoints.pre)
                return false;

            minion.GiveQuest();
            if (!minion.IsDoingQuest())
            {
                state.NextResendAttemptAt = Time.realtimeSinceStartup + PrestigeRetryDelaySeconds;
                LogOnce(state, "resend-not-processed", $"Resend for {GetDisplayName(minion)} was not processed.");
                return false;
            }

            if (afterPrestige || state.AwaitingPostPrestigeResend)
                NotifyOnce(state, "resent-after-prestige", $"{GetDisplayName(minion)} resent after prestige");

            state.NextResendAttemptAt = 0f;
            state.AwaitingPostPrestigeResend = false;
            state.HoldAfterPrestige = false;
            state.NotifiedTokens.Clear();
            state.LoggedTokens.Clear();
            return true;
        }
        catch (Exception exception)
        {
            state.NextResendAttemptAt = Time.realtimeSinceStartup + PrestigeRetryDelaySeconds;
            LogOnce(state, "resend-failed", $"Failed to resend {GetDisplayName(minion)}: {exception.Message}");
            return false;
        }
        finally
        {
            state.ResendInProgress = false;
        }
    }

    private static bool IsMaxLevel(Minion minion)
    {
        return minion.level >= minion.GetCurrentMaxLevel();
    }

    private static int GetDivinityPointsMultiplier()
    {
        if (Plugin.Settings?.DivinityPointsMultiplier == null)
            return 1;

        var configuredValue = Plugin.Settings.DivinityPointsMultiplier.Value;
        var clampedValue = Math.Clamp(configuredValue, 1, 100);
        if (configuredValue != clampedValue)
            Plugin.Settings.DivinityPointsMultiplier.Value = clampedValue;

        return clampedValue;
    }

    private static bool IsAutomaticPrestigeSelected(Minion minion)
    {
        if (Plugin.Settings == null || !Plugin.Settings.AutoPrestigeEnabled.Value)
            return false;

        return minion.id switch
        {
            WhiteKnightId => Plugin.Settings.PrestigeWhiteKnight.Value,
            DarkWizardId => Plugin.Settings.PrestigeDarkWizard.Value,
            StoneGolemId => Plugin.Settings.PrestigeStoneGolem.Value,
            SkeletonId => Plugin.Settings.PrestigeSkeleton.Value,
            GooMutantId => Plugin.Settings.PrestigeGooMutant.Value,
            CyclopsId => Plugin.Settings.PrestigeCyclops.Value,
            NinjaId => Plugin.Settings.PrestigeNinja.Value,
            _ => false
        };
    }

    private static string GetDisplayName(Minion minion)
    {
        try
        {
            var displayName = minion.GetCurrentEvolutionName();
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;
        }
        catch
        {
            // Fall back to the stable base-minion name below.
        }

        return minion.id switch
        {
            WhiteKnightId => "White Knight",
            DarkWizardId => "Dark Wizard",
            StoneGolemId => "Stone Golem",
            SkeletonId => "Skeleton",
            GooMutantId => "Goo Mutant",
            CyclopsId => "Cyclops",
            NinjaId => "Ninja",
            _ => "Minion"
        };
    }

    private static string GetMinionKey(Minion minion)
    {
        if (!string.IsNullOrEmpty(minion.id))
            return minion.id;

        return string.IsNullOrEmpty(minion.name) ? "unknown-minion" : minion.name;
    }

    private ProcessingState GetState(string minionId)
    {
        if (_states.TryGetValue(minionId, out var state))
            return state;

        state = new ProcessingState();
        _states[minionId] = state;
        return state;
    }

    private bool IsCurrentInstance()
    {
        return ReferenceEquals(Instance, this) && isActiveAndEnabled;
    }

    private bool IsUsable(Minion minion)
    {
        return IsCurrentInstance() && minion != null && Plugin.Settings != null;
    }

    private static void NotifyOnce(ProcessingState state, string token, string message)
    {
        if (!state.NotifiedTokens.Add(token) || Plugin.Settings == null ||
            !Plugin.Settings.ShowPrestigeNotifications.Value)
            return;

        Plugin.ModHelperInstance?.ShowNotification(message, true);
    }

    private static void LogOnce(ProcessingState state, string token, string message)
    {
        if (state.LoggedTokens.Add(token))
            Plugin.Logger.Warning(message);
    }

    private sealed class ProcessingState
    {
        internal bool ClaimInProgress;
        internal bool PrestigeInProgress;
        internal bool ResendInProgress;
        internal bool AwaitingPostPrestigeResend;
        internal bool HoldAfterPrestige;
        internal float NextClaimAttemptAt;
        internal float NextPrestigeAttemptAt;
        internal float NextResendAttemptAt;
        internal readonly HashSet<string> NotifiedTokens = new();
        internal readonly HashSet<string> LoggedTokens = new();
    }

    [HarmonyPatch(typeof(Minion), nameof(Minion.CanBeClaimed))]
    private static class MinionCanBeClaimedPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Minion __instance, bool __result)
        {
            if (__result)
                Instance?.TryClaim(__instance);
        }
    }

    [HarmonyPatch(typeof(Minion), nameof(Minion.IsStandingBy))]
    private static class MinionIsStandingByPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Minion __instance, bool __result)
        {
            if (__result)
                Instance?.HandleStandingBy(__instance);
        }
    }

    [HarmonyPatch(typeof(Minion), nameof(Minion.Prestige))]
    private static class MinionPrestigePatch
    {
        [HarmonyPostfix]
        private static void Postfix(ref double __result)
        {
            if (_automaticPrestigeRewardDepth > 0)
                __result *= GetDivinityPointsMultiplier();
        }
    }

}
