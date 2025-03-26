using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Reflection;
using System.Text;
using MelonLoader;
using Il2Cpp;

namespace KillMimics;

public class ChestKiller : MonoBehaviour
{
    public static ChestKiller Instance;

    private ChestHuntManager _chestHuntManager;
    private bool _chestOpenerCompleted;
    private bool _perfectChestOpenerCompleted;
    private bool _closeButtonLogged = false;

    private void Awake()
    {
        Instance = this;    
        _chestHuntManager = gameObject.GetComponentInChildren<ChestHuntManager>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            _chestHuntManager.StartEvent();
        }

        if (!GameState.IsChestHunt())
        {
            _chestOpenerCompleted = false;
            _perfectChestOpenerCompleted = false;
        }
        else
        {
            if (!_chestOpenerCompleted) 
            {
                if (!_chestHuntManager.IsVisible() || _chestHuntManager.chests.Count == 0)
                    return; // Exit early if chests are not ready

                Melon<Plugin>.Logger.Msg("Chest Event Started!");
                OpenChests();
            }

            _chestOpenerCompleted = true;

            if (!_perfectChestOpenerCompleted)
            {
                if (_chestHuntManager.perfectChests == null || _chestHuntManager.perfectChests.Count == 0)
                    return; // Exit early if perfect chests are not ready                                

                var PerfectChest = _chestHuntManager.perfectChests[2].perfectChestObject;
                if (PerfectChest == null )
                    return; // Exit early if perfect chest is not ready

                OpenPerfectChests();
                if (Debug.isDebugBuild)
                    Melon<Plugin>.Logger.Msg("Perfect Chests opened!");

                GameObject cb = _chestHuntManager.closeButton;
                if (cb != null)
                {
                    Image image = cb.GetComponent<Image>();
                    if (image != null && image.isActiveAndEnabled)
                    {
                        if (Debug.isDebugBuild)
                            Melon<Plugin>.Logger.Msg("Close button's Image component is active and enabled. Closing chest event.");
                        _chestHuntManager.Close();
                        _perfectChestOpenerCompleted = true;
                    }
                }
            }
        }
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
                    if (Debug.isDebugBuild)
                        Melon<Plugin>.Logger.Msg($"Opening chest of type: {chest.type}");
                    chestComponent.Open(true);
                }
                else
                {
                    if (Debug.isDebugBuild)
                        Melon<Plugin>.Logger.Msg($"ChestObject component not found on {chest.type}!");
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
            if (Debug.isDebugBuild)
                Melon<Plugin>.Logger.Msg($"Highest multiplier chest found with multiplier: {bestChest.multiplier}");
            var chestObj = bestChest.chestObject;
            if (chestObj != null)
            {
                ChestObject chestComponent = chestObj.GetComponent<ChestObject>();
                if (chestComponent != null)
                {
                    if (Debug.isDebugBuild)
                        Melon<Plugin>.Logger.Msg("Opening the highest multiplier chest.");
                    chestComponent.Open(true);
                }
                else
                {
                    if (Debug.isDebugBuild)
                        Melon<Plugin>.Logger.Msg("ChestObject component not found on the highest multiplier chest!");
                }
            }
            else
            {
                if (Debug.isDebugBuild)
                    Melon<Plugin>.Logger.Msg("Highest multiplier chest has no chestObject!");
            }
        }
        else
        {
            if (Debug.isDebugBuild)
                Melon<Plugin>.Logger.Msg("No multiplier chests found.");
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
                    if (Debug.isDebugBuild)
                        Melon<Plugin>.Logger.Msg($"Opening chest of type: {chest.type}");
                    chestComponent.Open(true);
                }
                else
                {
                    if (Debug.isDebugBuild)
                        Melon<Plugin>.Logger.Msg($"ChestObject component not found on {chest.type}!");
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
                if (Debug.isDebugBuild)
                    Melon<Plugin>.Logger.Msg("Opening perfect chest with multiplier 4...");
                var @object = perfectChest.perfectChestObject;
                if (!@object) continue;

                var perfectChestComponent = @object.GetComponent<PerfectChestObject>();
                if (perfectChestComponent != null)
                {
                    if (Debug.isDebugBuild)
                        Melon<Plugin>.Logger.Msg("Opening perfect chest...");
                    perfectChestComponent.Open(true);
                }
                else
                {
                    if (Debug.isDebugBuild)
                        Melon<Plugin>.Logger.Msg("PerfectChestObject component not found!");
                }
            }
        }
    }
}