using UnityEngine;
using UnityEngine.UI;
using Il2Cpp;

namespace PerfectChestHunter;

public class ChestKiller : MonoBehaviour
{
    public static ChestKiller Instance;

    private ChestHuntManager _chestHuntManager;
    private bool _chestOpenerCompleted;
    private bool _perfectChestOpenerCompleted;
    private bool _chestHuntClosed;
    private PlayerInventory _playerInventory;
    private bool DebugMode = false;

    private void Awake()
    {
        Instance = this;    
        _chestHuntManager = gameObject.GetComponentInChildren<ChestHuntManager>();
        _playerInventory = PlayerInventory.instance;
    }

    private void Update()
    {
#if DEBUG
        DebugMode = true;

        if (Input.GetKeyDown(KeyCode.O))
        {
            _chestHuntManager.StartEvent();

        }
#endif
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

                OpenChests();
            }

            _chestOpenerCompleted = true;

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
    }

    private void LowerStats()
    {
        _playerInventory.perfectChestHunts = (int)(_playerInventory.chestHunts * 0.006) - 1;
        _playerInventory.mimicsSlayed = (int)(_playerInventory.chestHunts * 2.5);
    }

    private void OpenChests()
    {
        foreach (var chest in _chestHuntManager.chests)
        {
            var @object = chest.chestObject;
            if (!@object) continue;
            if (chest.type == ChestType.DuplicateNextPick)
            {
                var chestComponent = @object.GetComponent<ChestObject>();
                if (chestComponent != null)
                {
                    if (DebugMode)
                        Plugin.Logger.Msg($"Opening chest of type: {chest.type}");
                    chestComponent.Open(true);
                }
                else
                {
                    if (DebugMode)
                        Plugin.Logger.Msg($"ChestObject component not found on {chest.type}!");
                }
            }
        }

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
            if (chestObj != null)
            {
                ChestObject chestComponent = chestObj.GetComponent<ChestObject>();
                if (chestComponent != null)
                {
                    if (DebugMode)
                        Plugin.Logger.Msg("Opening the highest multiplier chest.");
                    chestComponent.Open(true);
                }
                else
                {
                    if (DebugMode)
                        Plugin.Logger.Msg("ChestObject component not found on the highest multiplier chest!");
                }
            }
            else
            {
                if (DebugMode)
                    Plugin.Logger.Msg("Highest multiplier chest has no chestObject!");
            }
        }
        else
        {
            if (DebugMode)
                Plugin.Logger.Msg("No multiplier chests found.");
        }

        foreach (var chest in _chestHuntManager.chests)
        {
            var @object = chest.chestObject;
            if (!@object) continue;

            if (chest.type != ChestType.Mimic)
            {
                var chestComponent = @object.GetComponent<ChestObject>();
                if (chestComponent != null)
                {
                    if (DebugMode)
                        Plugin.Logger.Msg($"Opening chest of type: {chest.type}");
                    chestComponent.Open(true);
                }
                else
                {
                    if (DebugMode)
                        Plugin.Logger.Msg($"ChestObject component not found on {chest.type}!");
                }
            }
        }
    }

    private void OpenPerfectChests()
    {
        foreach (var perfectChest in _chestHuntManager.perfectChests)
        {
            if (perfectChest.multiplier == 4)
            {
                if (DebugMode)
                    Plugin.Logger.Msg("Opening perfect chest with multiplier 4...");
                var @object = perfectChest.perfectChestObject;
                if (!@object) continue;

                var perfectChestComponent = @object.GetComponent<PerfectChestObject>();
                if (perfectChestComponent != null)
                {
                    if (DebugMode)
                        Plugin.Logger.Msg("Opening perfect chest...");
                    perfectChestComponent.Open(true);
                }
                else
                {
                    if (DebugMode)
                        Plugin.Logger.Msg("PerfectChestObject component not found!");
                }
            }
        }
    }
}