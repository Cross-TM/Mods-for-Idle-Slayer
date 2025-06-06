﻿using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using HarmonyLib;

using MelonLoader;
using IdleSlayerMods.Common.Extensions;
using Il2Cpp;

namespace AutoAscendingHeights;

public class AutoAscendIngHeights : MonoBehaviour
{
    public static AutoAscendIngHeights Instance { get; private set; }

    // constants
    const float LeftPadX = 0.05f;
    const float RightPadX = 0.95f;
    const float PadY = 0.5f;
    const float HoldTime = 2f;

    // singletons
    AscendingHeightsController _ascendingCtrl;
    Divinity _higherAltitudes;
    PlayerMovement _pm;
    JumpPanel _jumpPanel;
    PlayerInventory _pi;

    Enemy _frozenSouls = Enemies.list.FrozenSouls;
    Enemy _frozenCoins = Enemies.list.FrozenCoins;


    Quest[] AscendingQuests;
    Upgrade[] UnlockNewQuests;

    // input pads
    PointerEventData _leftPad, _rightPad;

    // state
    bool _autoAscend = true;
    bool _holdingRight;

    float AscendingStartingHeight = -1;
    float MtOttoStartingHeight = -1;


    void Awake()
    {
        Instance = this;
        _ascendingCtrl = AscendingHeightsController.instance;
        _higherAltitudes = Divinities.list.HigherAltitudes;
        _pm = PlayerMovement.instance;
        _pi = PlayerInventory.instance;
    }

    void Start()
    {
        _jumpPanel = JumpPanel.instance;

        _leftPad = MakePad(LeftPadX);
        _rightPad = MakePad(RightPadX);

        if (AscendingStartingHeight == -1) AscendingStartingHeight = Maps.list.AscendingHeightsStage1.finishAtDistance;
        if (MtOttoStartingHeight == -1) MtOttoStartingHeight = Maps.list.MtOttoAscendingHeights.finishAtDistance;
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

        if (_autoAscend)
        {
            Maps.list.AscendingHeightsStage1.finishAtDistance = FinishHeight;
            Maps.list.MtOttoAscendingHeights.finishAtDistance = FinishHeight;

            if (_ascendingCtrl.FinishHeightReached() && _pm.IsGrounded())
            {
                if (!_holdingRight)
                {
                    _holdingRight = true;
                    MelonCoroutines.Start(HoldRightPad());
                }
            }
        }
        else
        {
            _ascendingCtrl.currentAscendingHeightsMap.finishAtDistance = AscendingStartingHeight;
            Maps.list.MtOttoAscendingHeights.finishAtDistance = MtOttoStartingHeight;
        }
    }

    float FinishHeight =>
        (_higherAltitudes.unlocked ? -975f : 25f);

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
            AutoAscendIngHeights.Instance.SkipSlider(__instance);
        }
    }

#if DEBUG
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            MapController.instance?.ChangeMap(Maps.list.AscendingHeightsStage1);
        }
    }

#endif
}