using UnityEngine;
using System.Reflection;
using MelonLoader;
using Il2Cpp;

namespace MinionManaging
{
    public class MinionSender : MonoBehaviour
    {
        public static MinionSender Instance { get; private set; }
        public static AscensionManager _ascensionManager => AscensionManager.instance;
        private bool _minionSenderEnabled;
        private bool IsRunning = true;
        private bool DebugMode;

        public bool MinionSenderEnabled
        {
            get { return _minionSenderEnabled; }
            private set { _minionSenderEnabled = value; }
        }

        private void Awake()
        {
            #if DEBUG
                DebugMode = true;
            #endif

            Instance = this;
            MinionSenderEnabled = true;

            if (Debug.isDebugBuild)
                Plugin.Logger.Msg("MinionSender Awake() called");
        }

        private void Start()
        {
            _ascensionManager.OpenMinionsPanel();
        }
        private void LateUpdate()
        {
            if (!GameState.IsRunner())
            {
                IsRunning = false;
            }
            else if (GameState.IsRunner() && !IsRunning)
            {
                IsRunning = true;
                _ascensionManager.OpenMinionsPanel();
            }
        }

        public void SendMinions()
        {
            Plugin.Logger.Msg("Sending Minions to work");
            MinionManager.instance.SendAll();
        }

        public void ClaimMinions()
        {
            Plugin.Logger.Msg("Claiming Minions");
            MinionManager.instance.ClaimAll();
        }

    }
}