﻿using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using HarmonyLib;

using MelonLoader;
using IdleSlayerMods.Common.Extensions;
using Il2Cpp;
using UnityEngine.SceneManagement;

namespace AutoAscendingHeights;

public class AutoAscend : MonoBehaviour
{
    public static AutoAscend Instance { get; private set; }

    // constants
    const float LeftPadX = 0.05f;
    const float RightPadX = 0.95f;
    const float PadY = 0.5f;
    const float HoldTime = 2f;
    float StartingBoost;

    // singletons
    AscendingHeightsController _ascendingCtrl;
    MapController _mapCtrl;
    Maps _maps;
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
    bool _longAscendingHeights;
    bool _ascendingHeightsQuestActive;

    void Awake()
    {
        Instance = this;
        _ascendingCtrl = AscendingHeightsController.instance;
        _mapCtrl = MapController.instance;
        _maps = Maps.list;
        _higherAltitudes = Divinities.list.HigherAltitudes;
        _pm = PlayerMovement.instance;
        _pi = PlayerInventory.instance;
    }

    void Start()
    {
        _jumpPanel = JumpPanel.instance;

        _leftPad = MakePad(LeftPadX);
        _rightPad = MakePad(RightPadX);

        StartingBoost = _ascendingCtrl.startingBoost;

        var AllQuests = _pi.allQuests;

        foreach (Quest quest in AllQuests) { 
        
            if (quest.questType == QuestType.KillEnemies && (quest.enemyToKill == _frozenCoins || quest.enemyToKill == _frozenSouls))
            {
                AscendingQuests.AddItem(quest);
            }
        }

        var AllUpgrades = _pi.upgrades;

        foreach (Upgrade upgrade in AllUpgrades)
        {
            if (upgrade.name.Contains("quest") && upgrade.bought == false)
            {
                UnlockNewQuests.AddItem(upgrade);
            }
        }
    }

    void CheckQuests() {
        bool questActive = false;

        foreach (Quest quest in AscendingQuests)
        {
            if (quest == null) continue;

            if (quest.CanBeCompleted())
            {
                questActive = true;
                break;
            }
        }

        _longAscendingHeights = questActive;
    }

    void LateUpdate()
    {
        //        CheckQuests();
//        _longAscendingHeights = true;

        if (_longAscendingHeights && _autoAscend)
            _ascendingCtrl.startingBoost = 600f;
        else
            _ascendingCtrl.startingBoost = StartingBoost;


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
            _ascendingCtrl.currentAscendingHeightsMap.finishAtDistance = FinishHeight;

            if (_ascendingCtrl.FinishHeightReached() && _pm.IsGrounded())
            {
                if (!_holdingRight)
                {
                    _holdingRight = true;
                    MelonCoroutines.Start(HoldRightPad());
                }
            }
        }
    }

    float FinishHeight =>
        _autoAscend ? (_longAscendingHeights ? 
        (_higherAltitudes.unlocked ? 2000f : 3000f) : 
        (_higherAltitudes.unlocked ? -975f : 25f)) : 1000f;


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
        if (Input.GetKeyDown(KeyCode.P))
        {
            _mapCtrl.ChangeMap(_maps.AscendingHeightsStage1);
        }
#endif
    }
}

/*
 * 
 * 
 * 
 * Quest.CanBeCompleted = start
 * Quest.CanBeClaimed = finish
 * 
 * PlayerInventory _pi
 * 
 * var AllQuests = _pi.allQuests
 * Enemy _frozenSouls = Enemies.list.FrozenSouls
 * Enemy _frozenCoins = Enemies.list.FrozenCoins
 * 
 * Quest[] AscendingQuests
 * 
 */