using Il2Cpp;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AutoRageMode;

public enum Mode
{
    Off,
    SoulsHorde,
    Horde,
    Auto
}

public class AutoRage : MonoBehaviour
{
    private const string RageCooldownTimerKey = "rageCooldown";
    private const string DelayedActivateTimerKey = "delayedActivate";
    private const string ShardsNecklaceName = "Shards Necklace";
    private const string SoulsCompassName = "Souls Compass";

    public static AutoRage Instance { get; private set; }

    private RageModeManager _rageMode;
    private Mode _currentMode = Mode.Off;
    private bool _rageNotUsed;
    private SoulsBonus _soulsEvent;
    private bool _hordeDuringSoulsEvent;
    private bool _megaHordeActivationPending;
    private readonly Dictionary<string, Coroutine> _timers = new();

    private void Awake()
    {
        Instance = this;
        Plugin.DLog("AutoRage Awake() called");

        int savedMode = Plugin.Settings.CurrentMode.Value;
        _currentMode = Enum.IsDefined(typeof(Mode), savedMode) ? (Mode)savedMode : Mode.Off;
        if ((int)_currentMode != savedMode)
            Plugin.Settings.CurrentMode.Value = (int)Mode.Off;

        _rageMode = RageModeManager.instance;
        Plugin.DLog(_rageMode == null
            ? "RageModeManager instance not found!"
            : "RageModeManager instance successfully found.");
    }

    private void LateUpdate()
    {
        if (Input.GetKeyDown(Plugin.Settings.RageToggleKey.Value))
            CycleMode();

        if (_rageMode == null)
            _rageMode = RageModeManager.instance;

        if (_hordeDuringSoulsEvent)
        {
            _hordeDuringSoulsEvent = false;
            ActivateRageModeDelayed();
        }

        if (_currentMode == Mode.SoulsHorde && _soulsEvent != null && _soulsEvent.timeLeft > 0 && _rageMode != null)
        {
            double soulsTimer = _rageMode.GetDuration() + 3.0;
            if (_soulsEvent.timeLeft <= soulsTimer && CanActivateRageMode(_rageMode))
            {
                Plugin.DLog($"Souls event at {soulsTimer}s left, triggering Rage!");
                ActivateRage();
            }
        }

        if (_rageMode != null && _rageMode.currentCd == 0 && !_rageNotUsed && !_megaHordeActivationPending &&
            !_timers.ContainsKey(RageCooldownTimerKey) && _soulsEvent == null)
        {
            Plugin.DLog("Rage cooldown reached zero, starting 5-minute countdown.");
            StartTimer(RageCooldownTimerKey, RageCooldownTimer());
        }

        if (_currentMode == Mode.Auto && !_megaHordeActivationPending && CanActivateRageMode(_rageMode))
        {
            Plugin.DLog("Regular Auto Rage Mode triggered!");
            ActivateRage();
        }

        if (_rageMode != null && _rageMode.currentCd > 0 &&
            (_rageNotUsed || _timers.ContainsKey(RageCooldownTimerKey)))
        {
            Plugin.DLog("Rage cooldown is no longer 0. Stopping Rage cooldown timer.");
            StopTimer(RageCooldownTimerKey);
            _rageNotUsed = false;
        }

        if (_soulsEvent?.timeLeft > 0)
            Plugin.DLog($"Souls Event Active - Time Left: {_soulsEvent.timeLeft} seconds");
    }

#if DEBUG
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            Plugin.DLog("Manual Souls event trigger!");
            SoulsBonus soulsEvent = FindObjectOfType<SoulsBonus>();
            if (soulsEvent != null)
            {
                soulsEvent.Activate(true);
                Plugin.DLog("Souls event activated manually!");
            }
            else
            {
                Plugin.DLog("Souls event not found!");
            }
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            Plugin.DLog("Manual Horde event trigger!");
            Horde hordeEvent = FindObjectOfType<Horde>();
            if (hordeEvent != null)
            {
                hordeEvent.Activate(true);
                Plugin.DLog("Horde event activated manually!");
            }
            else
            {
                Plugin.DLog("Horde event not found!");
            }
        }
    }
#endif

    private static Mode GetNextMode(Mode current)
    {
        var values = (Mode[])Enum.GetValues(typeof(Mode));
        int nextIndex = ((int)current + 1) % values.Length;
        return values[nextIndex];
    }

    private static string GetModeLabel(Mode mode) => mode switch
    {
        Mode.Auto => "Auto Rage Mode",
        Mode.Horde => "Horde Only Mode",
        Mode.SoulsHorde => "Souls Horde Only Mode",
        Mode.Off => "Auto Rage Mode",
        _ => mode.ToString()
    };

    private void CycleMode()
    {
        _currentMode = GetNextMode(_currentMode);
        Plugin.Settings.CurrentMode.Value = (int)_currentMode;

        bool isActive = _currentMode != Mode.Off;
        string message = isActive
            ? $"{GetModeLabel(_currentMode)} activated!"
            : "Auto Rage Mode deactivated!";

        Plugin.Logger.Msg($"{GetModeLabel(_currentMode)} is: {(isActive ? "ON" : "OFF")}");
        if (Plugin.Settings.ShowPopups.Value && Plugin.ModHelperInstance != null)
            Plugin.ModHelperInstance.ShowNotification(message, isActive);
    }

    private void StartTimer(string key, IEnumerator routine)
    {
        if (_timers.TryGetValue(key, out Coroutine old) && old != null)
            MelonCoroutines.Stop(old);

        Coroutine coroutine = (Coroutine)MelonCoroutines.Start(routine);
        _timers[key] = coroutine;
    }

    private void StopTimer(string key)
    {
        if (!_timers.TryGetValue(key, out Coroutine coroutine))
            return;

        if (coroutine != null)
            MelonCoroutines.Stop(coroutine);
        _timers.Remove(key);
    }

    private IEnumerator RageCooldownTimer()
    {
        yield return new WaitForSeconds(300f);
        _rageNotUsed = true;
        _timers.Remove(RageCooldownTimerKey);
        Plugin.DLog("Rage cooldown has been zero for 5 minutes.");
    }

    private IEnumerator DelayedRageModeActivation(bool megaHordeActivation = false)
    {
        yield return new WaitForSeconds(2f);
        ActivateRage();
        if (megaHordeActivation)
            _megaHordeActivationPending = false;
        _timers.Remove(DelayedActivateTimerKey);
    }

    public void SetActiveEvent(SoulsBonus eventInstance)
    {
        Plugin.DLog("Tracking Souls event timer...");
        _soulsEvent = eventInstance;
        StopTimer(RageCooldownTimerKey);
        _rageNotUsed = false;
    }

    public void ClearActiveEvent()
    {
        Plugin.DLog("Clearing Souls event timer...");
        _soulsEvent = null;
    }

    public void SetHordeEventActive()
    {
        if (_currentMode == Mode.SoulsHorde && _soulsEvent != null && _soulsEvent.timeLeft > 0)
        {
            Plugin.DLog("Horde event started during Souls event! Activating Rage Mode.");
            _hordeDuringSoulsEvent = true;
        }
        else if (_currentMode == Mode.SoulsHorde && _rageNotUsed)
        {
            Plugin.DLog("Horde event triggered after Rage cooldown expired - activating Rage!");
            ActivateRageModeDelayed();
        }
    }

    public bool IsSoulsHordeModeEnabled() => _currentMode == Mode.SoulsHorde;

    public bool IsHordeModeEnabled() => _currentMode == Mode.Horde;

    public bool ShouldResetRageOnMegaHorde() =>
        Plugin.Settings.ResetRageOnMegaHorde.Value ||
        Plugin.Settings.ResetRageAndActivateItemsOnMegaHorde.Value;

    public bool ShouldActivateItemsOnMegaHorde() =>
        Plugin.Settings.ResetRageAndActivateItemsOnMegaHorde.Value;

    public bool IsMegaHordeActive()
    {
        AscensionSkills skills = AscensionSkills.list;
        return skills?.MegaHorde != null && skills.MegaHorde.IsActive();
    }

    public void ActivateForMegaHorde()
    {
        _rageMode ??= RageModeManager.instance;
        if (_rageMode == null)
        {
            Plugin.Logger.Warning("Unable to activate Rage Mode: RageModeManager is unavailable.");
            return;
        }

        _rageMode.currentCd = 0;
        _rageNotUsed = false;
        StopTimer(RageCooldownTimerKey);
        Plugin.DLog("Rage Mode cooldown reset for Mega Horde.");

        if (ShouldActivateItemsOnMegaHorde())
        {
            TryActivateTemporaryItem(ShardsNecklaceName);
            TryActivateTemporaryItem(SoulsCompassName);
        }

        _megaHordeActivationPending = true;
        StartTimer(DelayedActivateTimerKey, DelayedRageModeActivation(megaHordeActivation: true));
    }

    private static bool TryActivateTemporaryItem(string expectedName)
    {
        PlayerInventory inventory = PlayerInventory.instance;
        if (inventory?.temporaryCraftableItems == null)
        {
            Plugin.Logger.Warning($"Unable to activate {expectedName}: temporary item inventory is unavailable.");
            return false;
        }

        foreach (TemporaryCraftableItem item in inventory.temporaryCraftableItems)
        {
            if (item == null || !MatchesItemName(item, expectedName))
                continue;

            try
            {
                if (item.IsActive())
                {
                    Plugin.DLog($"{expectedName} is already active.");
                    return true;
                }

                if (item.HowManyCanCraft() < 1d)
                {
                    Plugin.Logger.Warning($"Unable to activate {expectedName}: not enough materials or requirements are missing.");
                    return false;
                }

                item.Craft();
                Plugin.DLog($"{expectedName} activated for Mega Horde.");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger.Warning($"Unable to activate {expectedName}: {ex.Message}");
                return false;
            }
        }

        Plugin.Logger.Warning($"Unable to activate {expectedName}: item was not found.");
        return false;
    }

    private static bool MatchesItemName(TemporaryCraftableItem item, string expectedName) =>
        string.Equals(item.name, expectedName, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(item.localizedName, expectedName, StringComparison.OrdinalIgnoreCase);

    private void ActivateRage()
    {
        _rageMode ??= RageModeManager.instance;
        if (_rageMode == null)
        {
            Plugin.Logger.Warning("Unable to activate Rage Mode: RageModeManager is unavailable.");
            return;
        }

        _rageMode.Activate();
        _rageNotUsed = false;
        Plugin.DLog("Rage Mode activated.");
    }

    private static bool CanActivateRageMode(RageModeManager rageMode)
    {
        if (rageMode == null)
        {
            Plugin.DLog("RageModeManager is null inside CanActivateRageMode.");
            return false;
        }

        return rageMode.currentCd == 0;
    }

    public void ActivateRageModeDelayed()
    {
        _rageMode ??= RageModeManager.instance;
        if (!CanActivateRageMode(_rageMode))
            return;

        StartTimer(DelayedActivateTimerKey, DelayedRageModeActivation());
    }
}
