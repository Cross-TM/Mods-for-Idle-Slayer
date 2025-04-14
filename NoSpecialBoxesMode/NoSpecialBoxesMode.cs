using MelonLoader;
using UnityEngine;
using Il2Cpp;
using IdleSlayerMods.Common.Extensions;

namespace NoSpecialBoxesMode
{
    public class NoSpecialBoxes : MonoBehaviour
    {
        public static NoSpecialBoxes Instance { get; private set; }

        private bool _specialBoxesDisabled;
        private bool _completeBonusSlider;
        private bool _boxesToggledOnAgain = false;
        private BonusStartSlider _bonusSlider;

        private MapController _mapController;
        private PlayerInventory _playerInventory;

        private Maps _maps;


        private void Awake()
        {
            Instance = this;

            Plugin.Logger.Debug("NoSpecialBoxes Awake() called");

            _specialBoxesDisabled = false; // Default state (enabled)

            _mapController = GameObject.Find("Map").GetComponent<MapController>();

            _playerInventory = PlayerInventory.instance;

            _maps = Maps.list;
            if (_maps == null)
            {
                Plugin.Logger.Debug("Maps instance not initialized. Check your scene setup.");
            }
        }

        private void LateUpdate()
        {
#if DEBUG
            if (Input.GetKeyDown(KeyCode.P))
            {
                _mapController.ChangeMap(_mapController.CurrentBonusMap());
            }
            if (Input.GetKeyDown(KeyCode.O))
            {
                _mapController.ChangeMap(_maps.VictorBossFight);
            }
#endif

            if (_specialBoxesDisabled && _playerInventory.specialRandomBoxChance != 0)
            {
                _playerInventory.specialRandomBoxChance = 0f;
            }
            else if (!_specialBoxesDisabled && _boxesToggledOnAgain)
            {
                var currentTime = TimeManager.GetCurrentDateTime(false);
                double currentUnixTimeStamp = TimeManager.GetUnixTimeStampFromDate(currentTime);

                if (currentUnixTimeStamp - _mapController.specialRandomBoxLastUsed > 3600)
                {
                    _playerInventory.specialRandomBoxChance = 100f;
                }
                else
                {
                    _boxesToggledOnAgain = false;
                }
            }

            if (Input.GetKeyDown(Plugin.Settings.SpecialBoxesToggleKey.Value))
            {
                ToggleSetting("Special Boxes", ref _specialBoxesDisabled, Plugin.Settings.SpecialBoxesShowPopup.Value);
                TurnOffSetting("Bonus Mode Slider Bypass", ref _completeBonusSlider);
            }

            // Check if the user wants to skip the bonus slider
            if (Input.GetKeyDown(Plugin.Settings.BonusModeToggleKey.Value))
            {
                ToggleSetting("Bonus Mode Slider Bypass", ref _completeBonusSlider, Plugin.Settings.BonusModeShowPopup.Value);
                TurnOffSetting("Special Boxes", ref _specialBoxesDisabled);
            }
        }

        private void ToggleSetting(string type, ref bool state, bool showPopup)
        {
            state = !state;

            if (showPopup && type == "Special Boxes")
            {
                Plugin.Logger.Msg($"{type} are: {(state ? "OFF" : "ON")}");
                Plugin.ModHelperInstance.ShowNotification(state ? $"{type} disabled!" : $"{type} enabled!", state);
            }
            else if (showPopup && type == "Bonus Mode Slider Bypass")
            {
                Plugin.Logger.Msg($"{type} are: {(state ? "ON" : "OFF")}");
                Plugin.ModHelperInstance.ShowNotification(state ? $"{type} activated!" : $"{type} deactivated!", state);
            }

            if (type == "Special Boxes" && !state)
            {
                _boxesToggledOnAgain = true;
            }
            else if (type == "Special Boxes" && state)
            {
                _boxesToggledOnAgain = false;
            }
        }

        private void TurnOffSetting(string type, ref bool state)
        {
            state = false;
            if (type == "Special Boxes")
                Plugin.Logger.Debug($"{type} are enabled");
            else if (type == "Bonus Mode Slider Bypass")
                Plugin.Logger.Debug($"{type} is disabled");
        }

        public void SetBonusSlider(BonusStartSlider slider)
        {
            Plugin.Logger.Debug("BonusStartSlider instance registered.");

            if (GameState.IsBossFight())
            {
                Plugin.Logger.Debug("Boss Fight slider cannot be skipped currently");
                return;
            }
            else if (_completeBonusSlider && GameState.IsBonus())
            {
                _bonusSlider = slider;
                SkipBonusModeSlider();
            }
        }

        private void SkipBonusModeSlider()
        {

            if (_bonusSlider == null)
            {
                Plugin.Logger.Debug("No BonusStartSlider detected! Cannot skip slider.");
                return;
            }

            while (!_bonusSlider.sliderReady)
            {
                Plugin.Logger.Debug("Waiting for slider to be ready...");
            }

            if (_bonusSlider.confirmAction != null)
            {                
                Plugin.Logger.Debug("Invoking confirmAction...");
                _bonusSlider.confirmAction.Invoke();
            }
            else
            {
                Plugin.Logger.Debug("confirmAction is null, skipping puzzle might not work.");
            }

/*            if (GameState.IsBossFight())
            {
                Plugin.Logger.Debug("Boss Fight detected. Starting Boss Fight immediately.");
                _bossController.StartBossFight();
            }
*/            // Reset the reference to avoid keeping an old instance
            _bonusSlider = null;
            Plugin.Logger.Debug("BonusStartSlider reference cleared.");
        }
    }
}
