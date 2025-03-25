using UnityEngine;
using System.Collections;
using static UnityEngine.Random;

namespace QualityOfLifeMods
{
    public class MinionSender : MonoBehaviour
    {
        public static MinionSender Instance { get; private set; }
        private bool _minionSenderEnabled;
        public bool MinionSenderEnabled
        {
            get { return _minionSenderEnabled; }
            private set { _minionSenderEnabled = value; }
        }

        private void Awake()
        {
            Instance = this;
            Plugin.Log.LogDebug("MinionSender Awake() called");
        }
        private void Start()
        {
            Plugin.Log.LogDebug("Checking Minion Sender state on game start");
            if (MinionManager.instance != null)
            {
                Plugin.Log.LogDebug("MinionManager found. Activating Minion Sender");
                ToggleMinionSender("Minion Sender", ref _minionSenderEnabled, Plugin.Settings.MinionSenderShowPopup.Value);
            }
            else
            {
                Plugin.Log.LogError("MinionManager instance is null! Cannot activate Minion Sender.");
            }
        }
        private void LateUpdate()
        {
            if (Input.GetKeyDown(Plugin.Settings.MinionSenderToggleKey.Value))
            {
                ToggleMinionSender("Minion Sender", ref _minionSenderEnabled, Plugin.Settings.MinionSenderShowPopup.Value);
            }
        }
        private void ToggleMinionSender(string type, ref bool state, bool showPopup)
        {
            state = !state;
            Plugin.Log.LogInfo($"{type} is now: {(state ? "ON" : "OFF")}");
            if (showPopup)
                Plugin.ModHelperInstance.ShowNotification(state ? $"{type} activated!" : $"{type} deactivated!", state);
        }

        public void SendMinions()
        {
            Plugin.Log.LogInfo("Sending Minions to work");
            MinionManager.instance.SendAll();
        }

        public void ClaimMinions()
        {
            Plugin.Log.LogInfo("Claiming Minions");
            MinionManager.instance.ClaimAll();
        }

    }
}