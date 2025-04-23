using MelonLoader;
using System.Collections;
using UnityEngine;
using Il2Cpp;
using System.Diagnostics;
using System.Collections.Generic;

namespace AutoRageMode
{
    public enum Mode
    {
        Off,
        SoulsHorde,
        Horde,
        Auto
    }
    public class AutoRage : MonoBehaviour
    {
        public static AutoRage Instance { get; private set; }

        private RageModeManager _rageMode;
        private Mode _currentMode = Mode.Off;                               
        private bool _rageNotUsed = false;                                  
        private SoulsBonus soulsEvent;                                      
        private bool _hordeDuringSoulsEvent = false;                        
        private readonly Dictionary<string, Coroutine> _timers = new();     // Holds active coroutines by key so they can be cancelled cleanly


        private void Awake()
        {
            Instance = this;
            Plugin.DLog("AutoRage Awake() called");

            _currentMode = (Mode)Plugin.Settings.CurrentMode.Value;

            _rageMode = RageModeManager.instance;

            if (_rageMode == null)
            {
                Plugin.DLog("RageModeManager instance not found!");
            }
            else
            {
                Plugin.DLog("RageModeManager instance successfully found.");
            }
        }

        private void LateUpdate()
        {
            // User pressed the key—move to the next mode
            if (Input.GetKeyDown(Plugin.Settings.RageToggleKey.Value))
                CycleMode();

            // If a Horde event starts during a Souls event, activate Rage immediately
            if (_hordeDuringSoulsEvent)
            {
                _hordeDuringSoulsEvent = false;
                ActivateRageModeDelayed();
            }

            // SoulsHorde logic: trigger Rage when timer hits threshold
            if (_currentMode == Mode.SoulsHorde && soulsEvent != null && soulsEvent.timeLeft > 0)
            {
                double _soulsTimer = _rageMode.GetDuration() + 3.0;

                if (soulsEvent.timeLeft <= _soulsTimer && CanActivateRageMode(_rageMode))
                {
                    Plugin.DLog($"Souls event at {_soulsTimer}s left, triggering Rage!");
                    ActivateRage();
                }
            }

            // If RageMode cooldown reaches zero, start the 5-minute timer
            if (_rageMode != null && _rageMode.currentCd == 0 && !_rageNotUsed && !_timers.ContainsKey("rageCooldown") && soulsEvent == null)
            {
                Plugin.DLog("Rage cooldown reached zero, starting 5-minute countdown.");
                StartTimer("rageCooldown", RageCooldownTimer());
            }

            // Regular Auto Rage (If neither Horde Mode nor Souls-Horde Mode is enabled)
            if (_currentMode == Mode.Auto && CanActivateRageMode(_rageMode))
            {
                Plugin.DLog("Regular Auto Rage Mode triggered!");
                ActivateRage();
            }

            if (_rageMode != null && _rageMode.currentCd > 0 && (_rageNotUsed || _timers.ContainsKey("rageCooldown")))
            {
                Plugin.DLog("Rage cooldown is no longer 0. Stopping Rage cooldown timer.");
                StopTimer("rageCooldown");
                _rageNotUsed = false;
            }

            // Debugging: Log the cooldown of the Souls event if it's active
            if (soulsEvent?.timeLeft > 0)
            {
                Plugin.DLog($"Souls Event Active - Time Left: {soulsEvent.timeLeft} seconds");
            }

        }

        // Section used for debugging purposes - Triggers horde event manually
#if DEBUG
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                Plugin.DLog("Manual Souls event trigger!");
                var soulsEvent = GameObject.FindObjectOfType<SoulsBonus>();
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
                var hordeEvent = GameObject.FindObjectOfType<Horde>();
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
        // Advance to the next Mode in the enum (wrapping back to Off)
        private static Mode GetNextMode(Mode current)
        {
            var values = (Mode[])System.Enum.GetValues(typeof(Mode));
            int nextIndex = ((int)current + 1) % values.Length;
            return values[nextIndex];
        }

        // Human‑friendly label for each Mode
        private static string GetModeLabel(Mode mode) => mode switch
        {
            Mode.Auto => "Auto Rage Mode",
            Mode.Horde => "Horde Only Mode",
            Mode.SoulsHorde => "Souls Horde Only Mode",
            Mode.Off => "Auto Rage Mode",
            _ => mode.ToString()
        };

        // Cycle through Off → SoulsHorde → Horde → Auto → Off
        private void CycleMode()
        {
            // 1) Compute the next mode and whether we're turning it ON
            Mode previousMode = _currentMode;
            Mode newMode = GetNextMode(previousMode);
            bool isTurningOn = newMode != previousMode;

            // 2) Apply the toggle (off if we're hitting the same mode twice)
            _currentMode = isTurningOn ? newMode : Mode.Off;
            Plugin.Settings.CurrentMode.Value = (int)_currentMode;

            // 3) Build the message
            string label = GetModeLabel(_currentMode);
            string state = isTurningOn ? "ON" : "OFF";
            Plugin.Logger.Msg($"{label} is: {state}");

            // 4) Show popup if enabled
            if (Plugin.Settings.ShowPopups.Value)
            {
                bool isActive = _currentMode != Mode.Off;
                string popupText;
                if (_currentMode == Mode.Off)
                {
                    popupText = "Auto Rage Mode deactivated!";
                }
                else
                {
                    popupText = $"{GetModeLabel(_currentMode)} activated!";
                }
                Plugin.ModHelperInstance.ShowNotification(popupText, isActive);
            }
        }


        // Start a named coroutine, stopping any previous one with the same key
        private void StartTimer(string key, IEnumerator routine)
        {
            // Stop any existing
            if (_timers.TryGetValue(key, out var old) && old != null)
                StopCoroutine(old);

            // MelonCoroutines.Start returns object, so cast it
            Coroutine c = (Coroutine)MelonCoroutines.Start(routine);
            _timers[key] = c;
        }

        // Stop & remove a named coroutine if it exists
        private void StopTimer(string key)
        {
            if (_timers.TryGetValue(key, out var c) && c != null)
            {
                StopCoroutine(c);
                _timers.Remove(key);
            }
        }

        private IEnumerator RageCooldownTimer()
        {
            yield return new WaitForSeconds(300f); // 5 minutes
            _rageNotUsed = true;
            StopTimer("rageCooldown"); // Clear reference after completion
            Plugin.DLog("Rage cooldown has been zero for 5 minutes.");
        }

        private IEnumerator DelayedRageModeActivation()
        {
            yield return new WaitForSeconds(2f);
            ActivateRage();
            StopTimer("delayedActivate");
        }

        public void SetActiveEvent(SoulsBonus eventInstance)
        {
            Plugin.DLog("Tracking Souls event timer...");
            soulsEvent = eventInstance;
            StopTimer("rageCooldown");
            _rageNotUsed = false;

        }

        public void ClearActiveEvent()
        {
            Plugin.DLog("Clearing Souls event timer...");
            soulsEvent = null;
        }

        // Called externally from the Harmony patch when a Horde event starts
        public void SetHordeEventActive()
        {
            if (_currentMode == Mode.SoulsHorde && soulsEvent != null && soulsEvent.timeLeft > 0)
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

        // Checks if a Souls event is currently active
        public bool IsSoulsEventActive()
        {
            return soulsEvent != null && soulsEvent.timeLeft > 0;
        }

        public bool IsSoulsHordeModeEnabled()
        {
            return _currentMode == Mode.SoulsHorde;
        }
        public bool IsHordeModeEnabled()
        {
            return _currentMode == Mode.Horde;
        }
        private void ActivateRage()
        {
            _rageMode.Activate();
            _rageNotUsed = false;
            Plugin.DLog("Rage cooldown reset.");
        }
        private static bool CanActivateRageMode(RageModeManager rageMode)
        {
            if (rageMode == null)
            {
                Plugin.DLog("RageModeManager is null inside CanActivateRageMode.");
                return false;
            }

            return rageMode.currentCd == 0; // Only activate if cooldown is 0
        }
        public void ActivateRageModeDelayed()
        {
            if (_rageMode == null) return;
            if (_rageMode.currentCd > 0) return;

            StartTimer("delayedActivate", DelayedRageModeActivation());
        }

    }
}
