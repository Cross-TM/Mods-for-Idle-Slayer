using BepInEx.Unity.IL2CPP.Utils;
using System.Collections;
using UnityEngine;

namespace AutoRageMode
{
    public class AutoRage : MonoBehaviour
    {
        public static AutoRage Instance { get; private set; }

        private RageModeManager _rageMode;
        private bool _autoRageModeOnly; // Regular Auto Rage, activates when cooldown is 0
        private bool _hordeModeOnly;       // Activates only on Horde events
        private bool _soulsHordeModeOnly;  // Activates at 10s left in Souls event or if Horde happens during Souls event
        private bool _rageNotUsed = false;
        private SoulsBonus soulsEvent;     // Tracks the active Souls event
        private bool _hordeDuringSoulsEvent = false; // Tracks if a Horde event starts during a Souls event
        private Coroutine _rageCooldownCoroutine; // Store coroutine reference to stop it
        private double _soulsTimer; // Time left on Souls event when Rage should be activated
        private Coroutine _debugLogCoroutine; // Store reference to stop it if needed

        private void Awake()
        {
            Instance = this;
            Plugin.Log.LogDebug("AutoRage Awake() called");
            _rageMode = RageModeManager.instance;
            _soulsTimer = _rageMode.GetDuration() + 3.0;

            if (_rageMode == null)
            {
                Plugin.Log.LogError("RageModeManager instance not found!");
            }
            else
            {
                Plugin.Log.LogDebug("RageModeManager instance successfully found.");
            }

            _debugLogCoroutine = Instance.StartCoroutine(LogRageCooldownStatus());

            ToggleRageMode("Souls Horde Only Mode", ref _soulsHordeModeOnly, Plugin.Settings.SoulsHordeShowPopup.Value);

        }
        private IEnumerator LogRageCooldownStatus()
        {
            while (true) // Runs indefinitely
            {
                yield return new WaitForSeconds(60f); // Log every 60 seconds

                string rageModeStatus = _rageMode != null ? $"CD: {_rageMode.currentCd}" : "RageModeManager is NULL";
                string rageNotUsed = _rageNotUsed ? "YES" : "NO";
                string coroutineRunning = _rageCooldownCoroutine != null ? "RUNNING" : "NOT RUNNING";
                string soulsEventStatus = (soulsEvent != null) ? "Souls Event: ACTIVE" : "Souls Event: NULL";
                string soulsEventTimer = (soulsEvent != null && soulsEvent.timeLeft > 0) ? $"Time Left: {soulsEvent.timeLeft}s" : "No Active Timer";

                Plugin.Log.LogDebug($"[DEBUG] RageMode Status: {rageModeStatus} | RageNotUsed Status: {rageNotUsed} | Timer Running: {coroutineRunning} | Souls Event: {soulsEventStatus} | {soulsEventTimer}");
            }
        }

        private void LateUpdate()
        {
            // Toggle Auto Rage Mode (Activates whenever cooldown is 0)
            if (Input.GetKeyDown(Plugin.Settings.RageToggleKey.Value))
            {
                ToggleRageMode("Auto Rage Mode", ref _autoRageModeOnly, Plugin.Settings.RageShowPopup.Value);
                TurnOffRageMode("Souls Horde Only Mode", ref _soulsHordeModeOnly);
                TurnOffRageMode("Horde Only Mode", ref _hordeModeOnly);
            }

            // Toggle Horde-Only Mode (Activates only on Horde events)
            if (Input.GetKeyDown(Plugin.Settings.HordeToggleKey.Value))
            {
                ToggleRageMode("Horde Only Mode", ref _hordeModeOnly, Plugin.Settings.HordeShowPopup.Value);
                TurnOffRageMode("Auto Rage Mode", ref _autoRageModeOnly);
                TurnOffRageMode("Souls Horde Only Mode", ref _soulsHordeModeOnly);
            }

            // Toggle Souls-Horde Mode (Activates at 10s left or if Horde starts during a Souls event)
            if (Input.GetKeyDown(Plugin.Settings.SoulsHordeToggleKey.Value))
            {
                ToggleRageMode("Souls Horde Only Mode", ref _soulsHordeModeOnly, Plugin.Settings.SoulsHordeShowPopup.Value);
                TurnOffRageMode("Auto Rage Mode", ref _autoRageModeOnly);
                TurnOffRageMode("Horde Only Mode", ref _hordeModeOnly);
            }

            // If a Horde event starts during a Souls event, activate Rage immediately
            if (_hordeDuringSoulsEvent)
            {
                _hordeDuringSoulsEvent = false; // Reset flag after triggering Rage
                ActivateRageModeDelayed();
            }

            // If Souls event timeLeft reaches 10s and Souls-Horde Mode is active, activate Rage
            if (_soulsHordeModeOnly && soulsEvent != null && soulsEvent.timeLeft > 0)
            {
                if (soulsEvent.timeLeft <= _soulsTimer && CanActivateRageMode(_rageMode))
                {
                    Plugin.Log.LogDebug($"Souls event at {_soulsTimer}s left, triggering Rage!");
                    ActivateRage();
                }
            }

            // If RageMode cooldown reaches zero, start the 5-minute timer
            if (_rageMode != null && _rageMode.currentCd == 0 && !_rageNotUsed && _rageCooldownCoroutine == null && soulsEvent == null)
            {
                Plugin.Log.LogDebug("Rage cooldown reached zero, starting 5-minute countdown.");
                _rageCooldownCoroutine = Instance.StartCoroutine(RageCooldownTimer());
            }

            // Regular Auto Rage (If neither Horde Mode nor Souls-Horde Mode is enabled)
            if (!_hordeModeOnly && !_soulsHordeModeOnly && _autoRageModeOnly && CanActivateRageMode(_rageMode))
            {
                Plugin.Log.LogDebug("Regular Auto Rage Mode triggered!");
                ActivateRage();
            }

            if (_rageMode != null && _rageMode.currentCd > 0 && (_rageNotUsed || _rageCooldownCoroutine != null))
            {
                Plugin.Log.LogDebug("Rage cooldown is no longer 0. Stopping Rage cooldown timer.");
                StopRageCooldownTimer("Rage");
            }

            // Debugging: Log the cooldown of the Souls event if it's active
            if (soulsEvent != null && soulsEvent.timeLeft > 0)
            {
                Plugin.Log.LogDebug($"Souls Event Active - Time Left: {soulsEvent.timeLeft} seconds");
            }

        }

        // Section used for debugging purposes - Triggers horde event manually
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.J) && Plugin.Settings.DebugMode.Value)
            {
                Plugin.Log.LogDebug("Manual Souls event trigger!");
                var soulsEvent = GameObject.FindObjectOfType<SoulsBonus>();
                if (soulsEvent != null)
                {
                    soulsEvent.Activate(true);
                    Plugin.Log.LogDebug("Souls event activated manually!");
                }
                else
                {
                    Plugin.Log.LogError("Souls event not found!");
                }
            }

            if (Input.GetKeyDown(KeyCode.K) && Plugin.Settings.DebugMode.Value)
            {
                Plugin.Log.LogDebug("Manual Horde event trigger!");
                var hordeEvent = GameObject.FindObjectOfType<Horde>();
                if (hordeEvent != null)
                {
                    hordeEvent.Activate(true);
                    Plugin.Log.LogDebug("Horde event activated manually!");
                }
                else
                {
                    Plugin.Log.LogError("Horde event not found!");
                }
            }

        }

        private void ActivateRage()
        {
            _rageMode.Activate();
            _rageNotUsed = false;
            Plugin.Log.LogDebug("Rage cooldown reset.");
        }

        private IEnumerator RageCooldownTimer()
        {
            yield return new WaitForSeconds(300f); // 5 minutes
            _rageNotUsed = true;
            _rageCooldownCoroutine = null; // Clear reference after completion
            Plugin.Log.LogDebug("Rage cooldown has been zero for 5 minutes.");
        }
        private void StopRageCooldownTimer(string gameEvent)
        {
            if (_rageCooldownCoroutine != null)
            {
                StopCoroutine(_rageCooldownCoroutine);
                _rageCooldownCoroutine = null;
                Plugin.Log.LogDebug($"{gameEvent} event started - Stopping Rage cooldown timer.");
            }
            _rageNotUsed = false; // Prevent cooldown-based activation
        }

        private static bool CanActivateRageMode(RageModeManager rageMode)
        {
            if (rageMode == null)
            {
                Plugin.Log.LogError("RageModeManager is null inside CanActivateRageMode.");
                return false;
            }

            return rageMode.currentCd == 0; // Only activate if cooldown is 0
        }

        public void ActivateRageModeDelayed()
        {
            if (_rageMode == null) return;
            if (_rageMode.currentCd > 0) return;

            Instance.StartCoroutine(DelayedRageModeActivation());
        }

        private IEnumerator DelayedRageModeActivation()
        {
            yield return new WaitForSeconds(2f);
            ActivateRage();
        }

        private static void ToggleRageMode(string type, ref bool state, bool showPopup)
        {
            state = !state;
            Plugin.Log.LogInfo($"{type} is: {(state ? "ON" : "OFF")}");

            if (showPopup)
                Plugin.ModHelperInstance.ShowNotification(state ? $"{type} activated!" : $"{type} deactivated!", state);
        }

        private static void TurnOffRageMode(string type, ref bool state)
        {
            state = false;
            Plugin.Log.LogDebug($"{type} is: {(state ? "ON" : "OFF")}");
        }

        public void SetActiveEvent(SoulsBonus eventInstance)
        {
            Plugin.Log.LogDebug("Tracking Souls event timer...");
            soulsEvent = eventInstance;
            StopRageCooldownTimer("Souls"); // Stop cooldown timer when Souls event starts
        }

        public void ClearActiveEvent()
        {
            Plugin.Log.LogDebug("Clearing Souls event timer...");
            soulsEvent = null;
        }

        // Called externally from the Harmony patch when a Horde event starts
        public void SetHordeEventActive()
        {
            if (_soulsHordeModeOnly && soulsEvent != null && soulsEvent.timeLeft > 0)
            {
                Plugin.Log.LogDebug("Horde event started during Souls event! Activating Rage Mode.");
                _hordeDuringSoulsEvent = true;
            }
            else if (_soulsHordeModeOnly && _rageNotUsed)
            {
                Plugin.Log.LogDebug("Horde event triggered after Rage cooldown expired - activating Rage!");
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
            return _soulsHordeModeOnly;
        }
        public bool IsHordeModeEnabled()
        {
            return _hordeModeOnly;
        }
    }
}
