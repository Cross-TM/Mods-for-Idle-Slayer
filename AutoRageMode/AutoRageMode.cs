using BepInEx.Unity.IL2CPP.Utils;
using System.Collections;
using UnityEngine;

namespace AutoRageMode;

public class AutoRage : MonoBehaviour 
{
    public static AutoRage Instance { get; private set; }

    private RageModeManager _rageMode;
    private bool _autoRageModeEnabled;
    private bool _hordeModeOnly;

    private void Awake()
    {
        Instance = this;
        Plugin.Log.LogDebug("AutoRage Awake() called");
        _rageMode = RageModeManager.instance;

        if (_rageMode == null)
        {
            Plugin.Log.LogError("RageModeManager instance not found!");
        }
        else
        {
            Plugin.Log.LogDebug("RageModeManager instance successfully found.");
        }
    }

    private void LateUpdate()
    {

        if (Input.GetKeyDown(Plugin.Settings.RageToggleKey.Value))
        {
            ToggleRageMode("Auto Rage Mode", ref _autoRageModeEnabled, Plugin.Settings.RageShowPopup.Value);
        }

        if (Input.GetKeyDown(Plugin.Settings.HordeToggleKey.Value))
        {
            ToggleRageMode("Horde Only Mode", ref _hordeModeOnly, Plugin.Settings.HordeShowPopup.Value);
        }

        if (!_hordeModeOnly)
        {
            if (CanActivateRageMode(_rageMode, _autoRageModeEnabled))
            {
                Plugin.Log.LogDebug("Attempting to activate Auto Rage Mode...");
                _rageMode.Activate();
            }
        }
    }

    
    /*
     * // Section used for debugging purposes - Triggers horde event manually
       private void Update()
       {
            if (Input.GetKeyDown(KeyCode.J))
            {
                Plugin.Log.LogInfo("Manual Horde event trigger!");
                var hordeEvent = GameObject.FindObjectOfType<Horde>();
                if (hordeEvent != null)
                {
                    hordeEvent.Activate(true);
                    Plugin.Log.LogInfo("Horde event activated manually!");
                }
                else
                {
                    Plugin.Log.LogError("Horde event not found!");
                }
            }
        }
    */

    private static bool CanActivateRageMode(RageModeManager rageMode, bool state)
    {
        if (rageMode == null)
        {
            Plugin.Log.LogError("RageModeManager is null inside CanActivateRageMode.");
            return false;
        }

        double cd = rageMode.currentCd;
        Plugin.Log.LogDebug($"Checking if Rage Mode can activate. State: {state}, Cooldown: {cd}");

        return state && cd == 0;
    }

    public void ActivateRageModeForHorde()
    {
        if (!_hordeModeOnly) return; 
        if (!_autoRageModeEnabled) return; 
        if (_rageMode == null) return; 
        if (_rageMode.currentCd > 0) return;

        Instance.StartCoroutine(Instance.DelayedRageModeActivation());
    }

    private IEnumerator DelayedRageModeActivation()
    {
        //Plugin.Log.LogDebug("Horde Mode started! Activating Auto Rage Mode...");
        yield return new WaitForSeconds(2f);
        _rageMode.Activate();
    }


    private static void ToggleRageMode(string type, ref bool state, bool showPopup)
    {
        state = !state;
        Plugin.Log.LogInfo($"{type} is: {(state ? "ON" : "OFF")}");

        if (showPopup)
            Plugin.ModHelperInstance.ShowNotification(state ? $"{type} activated!" : $"{type} deactivated!", state);
    }
}