using UnityEngine;
using UnityEngine.UI;
using Il2Cpp;
using System.Collections;
using MelonLoader;
using static Il2Cpp.CasinoManager;

namespace PerfectChestHunter;

public class ChestKiller : MonoBehaviour
{
    public static ChestKiller Instance;

    private ChestHuntManager _chestHuntManager;
    private bool _chestOpenerCompleted;
    private bool _perfectChestOpenerCompleted;
    private bool _chestHuntClosed;
    private PlayerInventory _playerInventory;
    private bool DebugMode;
    private bool MultiplyArmoryChests;

    Achievement killMimic;
    Achievement perfectChestHunt;
    Achievement fivePerfectChestHunt;

    private bool killMimicCompleted;
    private bool perfectChestHuntCompleted;
    private bool fivePerfectChestHuntCompleted;

    private void Awake()
    {
        Instance = this;    
        _chestHuntManager = gameObject.GetComponentInChildren<ChestHuntManager>();
        _playerInventory = PlayerInventory.instance;

        MultiplyArmoryChests = Plugin.Settings.MultiplyArmoryChests.Value;
        InitialiseAchievements();
    }

    private void InitialiseAchievements() 
    {

        foreach (Achievement achievement in _playerInventory.achievements)
        {
            if (achievement.name == "achievement_that_was_fun")
            {
                killMimic = achievement;
                killMimicCompleted = killMimic.unlocked;
            }
            else if (achievement.name == "achievement_perfect_chest_hunt")
            {
                perfectChestHunt = achievement;
                perfectChestHuntCompleted = perfectChestHunt.unlocked;
            }
            else if (achievement.name == "achievement_locksmith")
            {
                fivePerfectChestHunt = achievement;
                fivePerfectChestHuntCompleted = fivePerfectChestHunt.unlocked;
            }
        }

    }

    private void LateUpdate()
    {
#if DEBUG
        DebugMode = true;

        if (Input.GetKeyDown(KeyCode.O) && !GameState.IsChestHunt())
        {
            _chestHuntManager.StartEvent();

        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            killMimic.unlocked = false;
            killMimicCompleted = false;

            _playerInventory.perfectChestHunts = 0;
            perfectChestHunt.unlocked = false;
            fivePerfectChestHunt.unlocked = false;
            perfectChestHuntCompleted = false;
            fivePerfectChestHuntCompleted = false;
        }
#endif

    }

    private void Update()
    {

        if (!GameState.IsChestHunt())
        {
            _chestOpenerCompleted = false;
            _perfectChestOpenerCompleted = false;
            _chestHuntClosed = false;
            killMimicCompleted = killMimic.unlocked;
        }
        else
        {
            if (!_chestOpenerCompleted)
            {
                if (!_chestHuntManager.IsVisible() || _chestHuntManager.chests.Count == 0)
                    return; // Exit early if chests are not ready

                Plugin.Logger.Msg("Chest Event Started!");
                LowerStats();

                if (!killMimicCompleted)
                {
                    MelonCoroutines.Start(OpenMimicChest());
                    _perfectChestOpenerCompleted = true;
                }
                else 
                    MelonCoroutines.Start(OpenChests());

                _chestOpenerCompleted = true;
            }

            if (!_perfectChestOpenerCompleted)
            {
                if (_chestHuntManager.perfectChests == null || _chestHuntManager.perfectChests.Count == 0)
                    return; // Exit early if perfect chests are not ready                                

                var PerfectChest = _chestHuntManager.perfectChests[2].perfectChestObject;
                if (PerfectChest == null)
                    return; // Exit early if perfect chest is not ready

                OpenPerfectChests();
                if (DebugMode)
                    Plugin.Logger.Msg("Perfect Chests opened!");
                _perfectChestOpenerCompleted = true;
            }

            if (!_chestHuntClosed)
            {
                GameObject cb = _chestHuntManager.closeButton;
                if (cb != null)
                {
                    AnimatedButton animatedButton = cb.GetComponent<AnimatedButton>();
                    if (animatedButton != null && animatedButton.isActiveAndEnabled)
                    {
                        _chestHuntManager.Close();
                        _chestHuntClosed = true;
                    }
                }
            }
        }

        if (Input.GetKeyDown(Plugin.Settings.ArmoryChestMultiplyToggleKey.Value))
        {
            ToggleSetting("Armory Chest Multiplying", ref MultiplyArmoryChests);
        }
    }

    private void ToggleSetting(string type, ref bool state)
    {
        state = !state;

        Plugin.Logger.Msg($"{type} are: {(state ? "ON" : "OFF")}");
        Plugin.ModHelperInstance.ShowNotification(state ? $"{type} enabled!" : $"{type} disabled!", state);

        Plugin.Settings.MultiplyArmoryChests.Value = state;
    }


    private void LowerStats()
    {
        fivePerfectChestHuntCompleted = fivePerfectChestHunt.unlocked;

        if (fivePerfectChestHuntCompleted)
        {
            _playerInventory.perfectChestHunts = (int)(_playerInventory.chestHunts * 0.006) - 1;
            if (_playerInventory.perfectChestHunts < 4)
            {
                _playerInventory.perfectChestHunts = 4;
            }
            _playerInventory.mimicsSlayed = (int)(_playerInventory.chestHunts * 2.5);
        }
    }

    private float defaultChestOpeningDelay = 0.25f; // Delay between opening chests

    private IEnumerator OpenMimicChest() 
    {
        int i = 0;

        foreach (var chest in _chestHuntManager.chests)
        {
            if (i >= 3) // Limit to opening only 3 Mimic chests
            {
                break;
            }
            var chestObj = chest.chestObject;
            if (!chestObj) continue;
            if (chest.type == ChestType.Mimic)
            {
                if (chest.opened) continue;
                if (DebugMode)
                    Plugin.Logger.Msg($"Processing chest of type: {chest.type}");
                yield return OpenChest(chestObj, defaultChestOpeningDelay);
                i++;
            }
        }
    }

    private IEnumerator OpenChest(ChestObject chestObj, float chestOpeningDelay)
    {
        var chestComponent = chestObj.GetComponent<ChestObject>();
        if (chestComponent != null)
        {
            if (DebugMode)
                Plugin.Logger.Msg($"Opening chest");
            chestComponent.Open(true);
        }
        else
        {
            if (DebugMode)
                Plugin.Logger.Msg($"ChestObject component not found!");
        }

        yield return new WaitForSeconds(chestOpeningDelay);
    }

    private IEnumerator OpenChests()
    {
        bool DupeOpened = false;

        // First segment: Opening DuplicateNextPick chests
        foreach (var chest in _chestHuntManager.chests)
        {
            var chestObj = chest.chestObject;
            if (!chestObj) continue;
            if (chest.type == ChestType.DuplicateNextPick)
            {
                if (chest.opened) continue;
                if (DebugMode)
                    Plugin.Logger.Msg($"Processing chest of type: {chest.type}");

                if (!DupeOpened)
                {
                    DupeOpened = true;
                    yield return OpenChest(chestObj, defaultChestOpeningDelay);
                }
                else
                {
                    yield return OpenChest(chestObj, 3f);
                }
            }
        }

        if (Plugin.Settings.MultiplyArmoryChests.Value)
        {
            // Second segment: Opening Armory chests
            foreach (var chest in _chestHuntManager.chests)
            {
                var chestObj = chest.chestObject;
                if (!chestObj) continue;
                if (chest.type == ChestType.ArmoryChest)
                {
                    if (chest.opened) continue;

                    if (DebugMode)
                        Plugin.Logger.Msg($"Processing chest of type: {chest.type}");
                    yield return OpenChest(chestObj, defaultChestOpeningDelay);
                }
            }
        }

        // Second or third segment: Opening the best multiplier chest
        Chest bestChest = null;
        int bestMultiplier = int.MinValue;

        foreach (var chest in _chestHuntManager.chests)
        {
            if (chest.type == ChestType.Multiplier)
            {
                if (chest.multiplier > bestMultiplier)
                {
                    bestMultiplier = chest.multiplier;
                    bestChest = chest;
                }
            }
        }

        if (bestChest != null)
        {
            if (DebugMode)
                Plugin.Logger.Msg($"Highest multiplier chest found with multiplier: {bestChest.multiplier}");

            var chestObj = bestChest.chestObject;
            if (chestObj != null && !bestChest.opened)
            {
                if (DebugMode)
                    Plugin.Logger.Msg("Opening the highest multiplier chest.");
                yield return OpenChest(chestObj, defaultChestOpeningDelay);
            }
        }

        // Third segment: Opening all non-mimic chests
        foreach (var chest in _chestHuntManager.chests)
        {
            var chestObj = chest.chestObject;
            if (!chestObj) continue;
            if (chest.type != ChestType.Mimic)
            {
                if (chest.opened) continue;
                if (DebugMode)
                    Plugin.Logger.Msg($"Processing chest of type: {chest.type}");
                yield return OpenChest(chestObj, defaultChestOpeningDelay);
            }
        }
    }

    private void OpenPerfectChests()
    {
        foreach (var perfectChest in _chestHuntManager.perfectChests)
        {

            if (perfectChest.type == ChestType.PermanentUpgrade)
            {
                if (DebugMode)
                    Plugin.Logger.Msg("Opening perfect chest with multiplier 4...");

                var @object = perfectChest.perfectChestObject;
                if (!@object) continue;

                var perfectChestComponent = @object.GetComponent<PerfectChestObject>();
                if (DebugMode)
                    Plugin.Logger.Msg("Opening perfect chest...");
                perfectChestComponent?.Open(true);
                return;
            }
        }

        Chest bestChest = null;
        int bestMultiplier = int.MinValue;

        foreach (var chest in _chestHuntManager.perfectChests)
        {
            if (chest.multiplier > bestMultiplier)
            {
                bestMultiplier = chest.multiplier;
                bestChest = chest;
            }
        }

        if (bestChest != null)
        {
            if (DebugMode)
                Plugin.Logger.Msg($"Highest multiplier chest found with multiplier: {bestChest.multiplier}");

            var @object = bestChest.perfectChestObject;

            var perfectChestComponent = @object.GetComponent<PerfectChestObject>();
            if (DebugMode)
                Plugin.Logger.Msg("Opening perfect chest...");
            perfectChestComponent?.Open(true);
        }
    }

    private void LogAllAchievements()
    {
        foreach (Achievement achievement in _playerInventory.achievements)
        {
            Plugin.Logger.Msg($"Achievement: {achievement.localizedName}, Description: {achievement.GetDescription()}, actualName :{achievement.name}");
        }
    }
}