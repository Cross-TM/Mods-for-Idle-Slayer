using UnityEngine;
using UnityEngine.UI;
using Il2Cpp;
using System.Collections;
using MelonLoader;

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

    private void Awake()
    {
        Instance = this;    
        _chestHuntManager = gameObject.GetComponentInChildren<ChestHuntManager>();
        _playerInventory = PlayerInventory.instance;

        MultiplyArmoryChests = Plugin.Settings.MultiplyArmoryChests.Value;
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
            _chestHuntManager.Close();
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
        }
        else
        {
            if (!_chestOpenerCompleted)
            {
                if (!_chestHuntManager.IsVisible() || _chestHuntManager.chests.Count == 0)
                    return; // Exit early if chests are not ready

                Plugin.Logger.Msg("Chest Event Started!");
                if (DebugMode)
                    LowerStats();

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
                    Image image = cb.GetComponent<Image>();
                    if (image != null && image.isActiveAndEnabled)
                    {
                        if (DebugMode)
                            Plugin.Logger.Msg("Close button's Image component is active and enabled. Closing chest event.");
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
        _playerInventory.perfectChestHunts = (int)(_playerInventory.chestHunts * 0.006) - 1;
        _playerInventory.mimicsSlayed = (int)(_playerInventory.chestHunts * 2.5);
    }

    private float defaultChestOpeningDelay = 0.25f; // Delay between opening chests

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
}