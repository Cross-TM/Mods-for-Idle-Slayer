﻿using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using HarmonyLib;

using MelonLoader;
using IdleSlayerMods.Common.Extensions;
using Il2Cpp;

namespace AutoAscendingHeights;

public class AutoAscend : MonoBehaviour
{
    public static AutoAscend Instance { get; private set; }

    // constants
    const float LeftPadX = 0.05f;
    const float RightPadX = 0.95f;
    const float PadY = 0.5f;
    const float HoldTime = 2f;

    // singletons
    AscendingHeightsController _ascendingCtrl;
    MapController _mapCtrl;
    Maps _maps;
    Divinity _higherAltitudes;
    PlayerMovement _pm;
    JumpPanel _jumpPanel;

    // input pads
    PointerEventData _leftPad, _rightPad;

    // state
    bool _autoAscend = true;
    bool _holdingRight;

    void Awake()
    {
        Instance = this;
        _ascendingCtrl = AscendingHeightsController.instance;
        _mapCtrl = MapController.instance;
        _maps = Maps.list;
        _higherAltitudes = Divinities.list.HigherAltitudes;
        _pm = PlayerMovement.instance;
    }

    void Start()
    {
        _jumpPanel = JumpPanel.instance;
        if (EventSystem.current == null)
            Plugin.Logger.Error("An EventSystem is required.");

        _leftPad = MakePad(LeftPadX);
        _rightPad = MakePad(RightPadX);
    }

    void LateUpdate()
    {
        if (Input.GetKeyDown(Plugin.Config.AutoAscendingHeightsToggleKey.Value))
            ToggleAutoAscend();

        if (!GameState.IsAscendingHeights())
        {
            if (_holdingRight)
                _holdingRight = false;
            return;
        }
        _ascendingCtrl.currentAscendingHeightsMap.finishAtDistance = FinishHeight;

        if (_autoAscend && _ascendingCtrl.FinishHeightReached() && _pm.IsGrounded())
        {
            if (!_holdingRight)
            {
                _holdingRight = true;
                MelonCoroutines.Start(HoldRightPad());
            }
        }
    }

    float FinishHeight => _autoAscend
        ? (_higherAltitudes.unlocked ? -975f : 25f)
        : (_higherAltitudes.unlocked ? 2000f : 1000f);

    void ToggleAutoAscend()
    {
        _autoAscend = !_autoAscend;
        Plugin.Logger.Msg($"AutoAscend: {(_autoAscend ? "ON" : "OFF")}");
        if (Plugin.Config.AutoAscendingHeightsShowPopup.Value)
            Plugin.ModHelperInstance.ShowNotification(
                $"Auto Ascend {(_autoAscend ? "Enabled" : "Disabled")}",
                _autoAscend
            );
    }

    IEnumerator HoldRightPad()
    {
        _jumpPanel.OnPointerDown(_rightPad);
        yield return new WaitForSeconds(HoldTime);
        _jumpPanel.OnPointerUp(_rightPad);
    }

    static PointerEventData MakePad(float xFrac) => new PointerEventData(EventSystem.current)
    {
        position = new Vector2(Screen.width * xFrac, Screen.height * PadY)
    };


    public void SkipSlider(BonusStartSlider slider)
    {
        if (_autoAscend && GameState.IsAscendingHeights())
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
            AutoAscend.Instance.SkipSlider(__instance);
        }
    }

    public void Update()
    {
#if DEBUG
        // Left = Z, Right = X
        if (Input.GetKeyDown(KeyCode.Z)) _jumpPanel.OnPointerDown(_leftPad);
        if (Input.GetKeyUp(KeyCode.Z)) _jumpPanel.OnPointerUp(_leftPad);

        if (Input.GetKeyDown(KeyCode.X)) _jumpPanel.OnPointerDown(_rightPad);
        if (Input.GetKeyUp(KeyCode.X)) _jumpPanel.OnPointerUp(_rightPad);

        if (Input.GetKeyDown(KeyCode.P))
        {
            _mapCtrl.ChangeMap(_maps.AscendingHeightsStage1);
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            _mapCtrl.ChangeMap(_maps.Village);
        }

#endif

    }
}