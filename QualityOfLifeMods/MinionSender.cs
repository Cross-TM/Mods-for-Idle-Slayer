using UnityEngine;
using System.Reflection;
using MelonLoader;
using Il2Cpp;

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
            if (Debug.isDebugBuild)
                Melon<Plugin>.Logger.Msg("MinionSender Awake() called");
        }
        private void Start()
        {
            if (Debug.isDebugBuild)
                Melon<Plugin>.Logger.Msg("Checking Minion Sender state on game start");
            if (MinionManager.instance != null)
            {
                if (Debug.isDebugBuild)
                    Melon<Plugin>.Logger.Msg("MinionManager found. Activating Minion Sender");
                ToggleMinionSender("Minion Sender", ref _minionSenderEnabled, Plugin.Settings.MinionSenderShowPopup.Value);
            }
            else
            {
                if (Debug.isDebugBuild)
                    Melon<Plugin>.Logger.Msg("MinionManager instance is null! Cannot activate Minion Sender.");
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
            Melon<Plugin>.Logger.Msg($"{type} is now: {(state ? "ON" : "OFF")}");
            if (showPopup)
                Plugin.ModHelperInstance.ShowNotification(state ? $"{type} activated!" : $"{type} deactivated!", state);
        }

        public void SendMinions()
        {
            Melon<Plugin>.Logger.Msg("Sending Minions to work");
            MinionManager.instance.SendAll();
        }

        public void ClaimMinions()
        {
            Melon<Plugin>.Logger.Msg("Claiming Minions");
            MinionManager.instance.ClaimAll();
        }

    }
}