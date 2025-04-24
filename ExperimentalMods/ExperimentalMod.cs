using Il2Cpp;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using MelonLoader;
using System;
using System.Reflection;
using UnityEngine.UI;

namespace ExperimentalMods;

public class ExperimentalMod : MonoBehaviour
{
/*    public static ExperimentalMod Instance { get; private set; }

    private AscensionManager _ascensionManager;
    private AscendingHeightsController _ascendingHeightsController;
    private JumpPanel _jumpPanel;
    private PlayerMovement _playerMovement;


    private bool RegularAscendMode = false;
    private bool UltraAscendMode = true;
    private bool _loggerRunning = false;
    private bool jumpMode = false;

    public bool LoggerRunning
    {
        get => _loggerRunning;
        set => _loggerRunning = value;
    }   

    private void Awake()
    {
        Instance = this;

        _ascensionManager = AscensionManager.instance;

        if (_ascensionManager == null)
        {
            Plugin.Logger.Msg("AscensionManager instance is null!");
        }
        else
        {
            Plugin.Logger.Msg("AscensionManager successfully found!");
        }

        _ascendingHeightsController = AscendingHeightsController.instance;

        if (_ascendingHeightsController == null)
        {
            Plugin.Logger.Msg("AscendingHeightsController instance is null!");
        }
        else
        {
            Plugin.Logger.Msg("AscendingHeightsController successfully found!");
        }

        _ascendingHeightsController.boostPlatformForce = 300f;

        _jumpPanel = JumpPanel.instance;

        if (_jumpPanel == null)
        {
            Plugin.Logger.Msg("JumpPanel instance is null!");
        }
        else
        {
            Plugin.Logger.Msg("JumpPanel successfully found!");
        }

        _playerMovement = PlayerMovement.instance;

        if (_playerMovement == null)
        {
            Plugin.Logger.Msg("PlayerMovement instance is null!");
        }
        else
        {
            Plugin.Logger.Msg("PlayerMovement successfully found!");
        }

        MelonCoroutines.Start(ShootArrows());


    }


    public static void ToggleRandomBoxMagnetSkill(Boolean state)
    {
        // Get the ascension skills manager
        AscensionSkills asc = AscensionSkills.list;
        if (asc == null)
        {
            Plugin.Logger.Msg("ToggleRandomBoxMagnetSkill: AscensionSkills.list is null.");
            return;
        }

        // Retrieve the RandomBoxMagnet skill
        AscensionSkill skill = asc.RandomBoxMagnet;
        if (skill == null)
        {
            Plugin.Logger.Msg("ToggleRandomBoxMagnetSkill: Skill 'RandomBoxMagnet' not found.");
            return;
        }

        // Log the current active state
        bool isActive = skill.IsActive();
        Plugin.Logger.Msg($"ToggleRandomBoxMagnetSkill: 'RandomBoxMagnet' is currently active? {isActive}");

        // Get the associated skill object that handles UI/toggling
        AscensionSkillObject skillObj = skill.skillObjectComponent;
        if (skillObj == null)
        {
            Plugin.Logger.Msg("ToggleRandomBoxMagnetSkill: Associated AscensionSkillObject is null.");
            return;
        }

        // Toggle the state
        skillObj.SetActive(state);
        Plugin.Logger.Msg($"ToggleRandomBoxMagnetSkill: 'RandomBoxMagnet' has been toggled to active = {state}.");
    }




    private IEnumerator ShootArrows()
    {
        while (true)
        {
            try
            {
                //Plugin.Logger.Msg("Shooting arrows...");
                if (_playerMovement != null )
                {
                    if (jumpMode)
                        _playerMovement.ShootArrow();
                }
                else
                {
                    Plugin.Logger.Msg("_playerMovement is null.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.Msg("Error in ShootArrows: " + ex.Message);
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    // Create a dummy PointerEventData using the current EventSystem.
    PointerEventData eventData = new PointerEventData(EventSystem.current);

    private void TriggerJump()
    {
        // Log and simulate the pointer events.
        _jumpPanel.OnPointerDown(eventData);
        // Optionally, you might want a short delay before releasing.
        // For a delay, you could call this function via a coroutine.
        _jumpPanel.OnPointerUp(eventData);
    }

    private IEnumerator TriggerLargeJump()
    {
        // Log and simulate the pointer events.
        _jumpPanel.OnPointerDown(eventData);

        yield return new WaitForSeconds(0.2f);
        _jumpPanel.OnPointerUp(eventData);


    }

    private void LateUpdate()
    {
        if ((_playerMovement.IsGrounded() || _playerMovement.IsWalled() ) && jumpMode)
        {

            if (_playerMovement.isMoving)
            {
                TriggerJump();
            }
            else
            {
                MelonCoroutines.Start(TriggerLargeJump());
            }

        }

        if (Input.GetKeyDown(Plugin.Config.AscendToggleKey2.Value))
        {
            ToggleJumper(ref jumpMode);
        }


            if (Input.GetKeyDown(Plugin.Config.AscendToggleKey.Value))
        {
            Plugin.Logger.Msg("Z Pressed");
            //ToggleRandomBoxMagnetSkill(false);
            Plugin.Logger.Msg($"Game State Runner Mode is {GameState.IsRunner()}");
            Plugin.Logger.Msg($"Game State Bonus Mode is {GameState.IsBonus()}");

            //            if (_ascensionManager != null) 
            //                _ascensionManager.Ascend(RegularAscendMode);

            *//*            double currentLifetime = SlayerPoints.lifetime;
                        Plugin.Logger.Msg($"Current Lifetime: {currentLifetime}");

                        double currentPoints = SlayerPoints.pre;
                        Plugin.Logger.Msg($"Current Points: {currentPoints}");
            *//*

            //            ToggleLogger(ref _loggerRunning);
            //CompleteHyperModeQuest();

            *//*            // 1. Get the list of currently displayed (or valid) upgrades
                        Il2CppSystem.Collections.Generic.List<Upgrade> upgrades = UpgradesList.instance.lastScrollListData;

                        // 2. Track the cheapest upgrade
                        Upgrade cheapest = null;
                        double minCost = double.MaxValue;

                        // 3. Loop through them
                        foreach (var upg in upgrades)
                        {
                            // Make sure it's not already bought, not disabled, etc.
                            if (!upg.bought && !upg.disabled)
                            {
                                double cost = upg.GetCost();  // or upg.cost
                                if (cost < minCost)
                                {
                                    minCost = cost;
                                    cheapest = upg;
                                }
                            }
                        }

                        // 4. If we found one, check if requirements are met, then "buy" it
                        if (cheapest != null)
                        {
                            bool canBuy = UpgradesList.instance.CheckRequirements(cheapest);
                            if (canBuy)
                            {
                                // Do your coin check or other logic
                                // Then set it to bought
                                cheapest.bought = true;

                                // Possibly call UpgradesList.instance.RefreshList() 
                                // to update the UI or something similar.
                            }
                        }
            *//*



            //            ToggleRageMode("Auto Rage Mode", ref _autoRageModeOnly, Plugin.Settings.RageShowPopup.Value);
            //            TurnOffRageMode("Souls Horde Only Mode", ref _soulsHordeModeOnly);
            //            TurnOffRageMode("Horde Only Mode", ref _hordeModeOnly);
        }
    }
    private static void CompleteHyperModeQuest()
    {
        // Try to find the QuestsList in the scene.
        QuestsList questsList = UnityEngine.Object.FindObjectOfType<QuestsList>();
        if (questsList == null)
        {
            Plugin.Logger.Msg("QuestsList instance not found in scene.");
            return;
        }

        // Retrieve the lastScrollListData property (a List<Quest>).
        Il2CppSystem.Collections.Generic.List<Quest> questList = questsList.lastScrollListData;
        if (questList == null)
        {
            Plugin.Logger.Msg("QuestsList.lastScrollListData is null.");
            return;
        }

        // Loop over each quest and log its data.
        for (int i = 0; i < questList.Count; i++)
        {
            Quest quest = questList[i];
            if (quest == null)
            {
                Plugin.Logger.Msg($"Quest at index {i} is null.");
                continue;
            }

            if (quest.name == "quest_hyper_mode")
            {
                quest.AddProgress(quest.questGoal - quest.questCurrentGoal);
                return;
            }

        }
    }

    public static void LogQuestListData()
    {

        // Try to find the QuestsList in the scene.
        QuestsList questsList = UnityEngine.Object.FindObjectOfType<QuestsList>();
        if (questsList == null)
        {
            Plugin.Logger.Msg("QuestsList instance not found in scene.");
            return;
        }

        // Retrieve the lastScrollListData property (a List<Quest>).
        Il2CppSystem.Collections.Generic.List<Quest> questList = questsList.lastScrollListData;
        if (questList == null)
        {
            Plugin.Logger.Msg("QuestsList.lastScrollListData is null.");
            return;
        }

        Plugin.Logger.Msg($"Found {questList.Count} quests in QuestsList.lastScrollListData.");

        // Loop over each quest and log its data.
        for (int i = 0; i < questList.Count; i++)
        {
            Quest quest = questList[i];
            if (quest == null)
            {
                Plugin.Logger.Msg($"Quest at index {i} is null.");
                continue;
            }

            // Build a log message with selected properties.
            string logMessage =
                $"Quest {i}:\n" +
                $"  Name: {quest.name}\n" +
                $"  Localized Name: {quest.localizedName}\n" +
                $"  Description: {quest.description}\n" +
                $"  Quest Goal: {quest.questGoal}\n" +
                $"  Current Goal: {quest.questCurrentGoal}\n" +
                $"  Is Claimed: {quest.isClaimed}";
            Plugin.Logger.Msg(logMessage);
        }
    }


    private static void ToggleJumper(ref bool state)
    {
        state = !state;
        Plugin.Logger.Msg($"Logger is: {(state ? "ON" : "OFF")}");
    }

    *//*    private static void ToggleRageMode(string type, ref bool state, bool showPopup)
        {
            state = !state;
            Plugin.Logger.Msg($"{type} is: {(state ? "ON" : "OFF")}");

            if (showPopup)
                Plugin.ModHelperInstance.ShowNotification(state ? $"{type} activated!" : $"{type} deactivated!", state);
        }

        private static void TurnOffRageMode(string type, ref bool state)
        {
            state = false;
            if (Debug.isDebugBuild)
                Plugin.Logger.Msg($"{type} is: {(state ? "ON" : "OFF")}");
        }
    */
}