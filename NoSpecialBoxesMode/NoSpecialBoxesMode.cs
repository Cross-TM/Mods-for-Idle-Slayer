using MelonLoader;
using UnityEngine;
using Il2Cpp;

namespace NoSpecialBoxesMode
{
    public class NoSpecialBoxes : MonoBehaviour
    {
        public static NoSpecialBoxes Instance { get; private set; }

        private bool _specialBoxesDisabled;
        private bool _completeBonusSlider;
        private float _timer = 0f;
        private SpecialRandomBox _boxToReenable;
        private BonusStartSlider _bonusSlider;


        private void Awake()
        {
            Instance = this;

            if (Debug.isDebugBuild)
                Melon<Plugin>.Logger.Msg("NoSpecialBoxes Awake() called");

            _specialBoxesDisabled = false; // Default state (enabled)
        }

        private void Update()
        {
            if (_timer > 0)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0 && _boxToReenable != null)
                {
                    _boxToReenable.gameObject.SetActive(true);
                    if (Debug.isDebugBuild)
                        Melon<Plugin>.Logger.Msg($"Special Box re-enabled after delay.");
                    _boxToReenable = null;
                }
            }

        }

        private void LateUpdate()
        {
            if (Input.GetKeyDown(Plugin.Settings.SpecialBoxesToggleKey.Value))
            {
                ToggleSetting("Special Boxes", ref _specialBoxesDisabled, Plugin.Settings.SpecialBoxesShowPopup.Value);
                TurnOffSetting("Bonus Mode Slider Bypass", ref _completeBonusSlider);
            }

            // Check if the user wants to skip the bonus slider
            if (Input.GetKeyDown(Plugin.Settings.BonusModeToggleKey.Value))
            {
                //SkipBonusModeSlider();
                ToggleSetting("Bonus Mode Slider Bypass", ref _completeBonusSlider, Plugin.Settings.BonusModeShowPopup.Value);
                TurnOffSetting("Special Boxes", ref _specialBoxesDisabled);
            }
        }

        private void ToggleSetting(string type, ref bool state, bool showPopup)
        {
            state = !state;
            Melon<Plugin>.Logger.Msg($"{type} are: {(state ? "OFF" : "ON")}");

            if (showPopup && type == "Special Boxes")
            {
                Plugin.ModHelperInstance.ShowNotification(state ? $"{type} disabled!" : $"{type} enabled!", state);
            }
            else if (showPopup && type == "Bonus Mode Slider Bypass")
            {
                Plugin.ModHelperInstance.ShowNotification(state ? $"{type} activated!" : $"{type} deactivated!", state);
            }
        }

        private void TurnOffSetting(string type, ref bool state)
        {
            state = false;
            if (Debug.isDebugBuild)
                Melon<Plugin>.Logger.Msg($"{type} are: {(state ? "OFF" : "ON")}");
        }

        public void HandleSpecialBoxSpawn(SpecialRandomBox box)
        {
            if (!_specialBoxesDisabled)
            {
                if (Debug.isDebugBuild)
                    Melon<Plugin>.Logger.Msg("Special Boxes are enabled. No action taken.");
                return;
            }

            if (Debug.isDebugBuild)
                Melon<Plugin>.Logger.Msg("Special Box detected! Disabling...");

            _boxToReenable = box;
            box.gameObject.SetActive(false);
            _timer = 30f; // 30-second delay before re-enabling
        }

        public void SetBonusSlider(BonusStartSlider slider)
        {
            if (Debug.isDebugBuild)
                Melon<Plugin>.Logger.Msg("BonusStartSlider instance registered.");
            _bonusSlider = slider;
            SkipBonusModeSlider();
        }

        private void SkipBonusModeSlider()
        {
            if (_bonusSlider == null)
            {
                if (Debug.isDebugBuild)
                    Melon<Plugin>.Logger.Msg("No BonusStartSlider detected! Cannot skip slider.");
                return;
            }

            if (Debug.isDebugBuild)
                Melon<Plugin>.Logger.Msg("Skipping Bonus Mode Slider...");

            // Set slider to max and mark it as ready
            _bonusSlider.value = _bonusSlider.maxValue;
            _bonusSlider.sliderReady = true;

            if (_bonusSlider.confirmAction != null)
            {
                if (Debug.isDebugBuild)
                    Melon<Plugin>.Logger.Msg("Invoking confirmAction...");
                _bonusSlider.confirmAction.Invoke();
            }
            else
            {
                if (Debug.isDebugBuild)
                    Melon<Plugin>.Logger.Msg("confirmAction is null, skipping puzzle might not work.");
            }

            // Reset the reference to avoid keeping an old instance
            _bonusSlider = null;
            if (Debug.isDebugBuild)
                Melon<Plugin>.Logger.Msg("BonusStartSlider reference cleared.");
        }
    }
}
