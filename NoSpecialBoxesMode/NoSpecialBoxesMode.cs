using UnityEngine;
using Il2Cpp;
using IdleSlayerMods.Common.Extensions;
using HarmonyLib;

namespace NoSpecialBoxesMode
{
    public class NoSpecialBoxes : MonoBehaviour
    {
        public static NoSpecialBoxes Instance { get; private set; }

        private bool _specialBoxesEnabled;
        private bool _boxesToggledOnAgain;

        private MapController _mapController;
        private PlayerInventory _playerInventory;

        private Maps _maps;


        private void Awake()
        {
            Instance = this;

            Plugin.Logger.Debug("NoSpecialBoxes Awake() called");

            _mapController = MapController.instance;

            _playerInventory = PlayerInventory.instance;

            _specialBoxesEnabled = Plugin.Config.NoSpecialBoxesEnabled.Value;

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

            if (!_specialBoxesEnabled && _playerInventory.specialRandomBoxChance != 0)
            {
                _playerInventory.specialRandomBoxChance = 0f;
            }
            else if (_specialBoxesEnabled && _boxesToggledOnAgain)
            {
                var currentTime = TimeManager.GetCurrentDateTime(false);
                double currentUnixTimeStamp = TimeManager.GetUnixTimeStampFromDate(currentTime);

                if (currentUnixTimeStamp - _mapController.specialRandomBoxLastUsed > 7200)
                {
                    _playerInventory.specialRandomBoxChance = 100f;
                }
                else
                {
                    _boxesToggledOnAgain = false;
                }
            }

            if (Input.GetKeyDown(Plugin.Config.SpecialBoxesToggleKey.Value))
            {
                ToggleSetting("Special Boxes", ref _specialBoxesEnabled, Plugin.Config.SpecialBoxesShowPopup.Value);
            }
        }

        private void ToggleSetting(string type, ref bool state, bool showPopup)
        {
            state = !state;

            Plugin.Logger.Msg($"{type} are: {(state ? "ON" : "OFF")}");
            if (showPopup)
                Plugin.ModHelperInstance.ShowNotification(state ? $"{type} enabled!" : $"{type} disabled!", state);

            _boxesToggledOnAgain = state;
            Plugin.Config.NoSpecialBoxesEnabled.Value = state;
        }

        public void SkipSlider(BonusStartSlider slider)
        {
            if (GameState.IsBonus())
            {
                if (slider == null) return;

                while (!slider.sliderReady)
                {
                    Plugin.Logger.Debug("Waiting for slider to be ready...");
                }

                slider.confirmAction?.Invoke();
            }
        }

        [HarmonyPatch(typeof(BonusStartSlider), "SetRandomPuzzle")]
        public class BonusStartSliderPatch
        {
            [HarmonyPostfix]
            static void Postfix(BonusStartSlider __instance)
            {
                NoSpecialBoxes.Instance.SkipSlider(__instance);
                Plugin.Logger.Debug("Detected BonusStartSlider creation.");
            }
        }

    }
}