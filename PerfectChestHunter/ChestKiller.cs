using UnityEngine;
using Il2Cpp;
using System.Collections;
using MelonLoader;
using IdleSlayerMods.Common.Extensions;

namespace PerfectChestHunter;

public class ChestKiller : MonoBehaviour
{
    public static ChestKiller Instance;

    private Achievement killMimic;
    private ChestHuntManager _chestHuntManager;
    private PlayerInventory _playerInventory;
    private bool _chestOpenerRunning;
    private bool DebugMode;
    // Missing info must never make the hunt open a Mimic on purpose, so default to "already unlocked".
    private bool killMimicCompleted = true;
    private bool _achievementSearchDone;
    private bool MultiplyArmoryChests;
    private bool PerfectChestHuntOn;

    private float defaultChestOpeningDelay = 0.25f; // Delay between opening chests

    private System.Random _rng = new System.Random();

    private void Awake()
    {
        Instance = this;    
        _chestHuntManager = gameObject.GetComponentInChildren<ChestHuntManager>();
        _playerInventory = PlayerInventory.instance;

        if (_chestHuntManager == null)
            Plugin.Logger.Warning("ChestHuntManager not found under 'Chest Hunt'");

        MultiplyArmoryChests = Plugin.Settings.MultiplyArmoryChests.Value;
        PerfectChestHuntOn = Plugin.Settings.PerfectChestsHuntOn.Value;
    }

    private void Start() {

        Plugin.ModHelperInstance.CreateSettingsToggle("Open Chests Randomly/Perfectly", Plugin.Settings.PerfectChestsHuntOn,
            _ => {
                PerfectChestHuntOn = Plugin.Settings.PerfectChestsHuntOn.Value;
                Plugin.Settings.PerfectChestsHuntOn.SaveEntry();
            });
    }

    // Achievements are still empty when Awake runs (since the October 2026 game update), so search once they are loaded.
    private void TryFindKillMimicAchievement()
    {
        if (_playerInventory == null || _playerInventory.achievements == null || _playerInventory.achievements.Length == 0)
            return;

        _achievementSearchDone = true;

        Achievement lowestMimicsSlayed = null;
        foreach (Achievement achievement in _playerInventory.achievements)
        {
            if (achievement.name == "achievement_that_was_fun")
                killMimic = achievement;

            if (achievement.achievementType == AchievementType.MimicsSlayed
                && (lowestMimicsSlayed == null || achievement.target < lowestMimicsSlayed.target))
                lowestMimicsSlayed = achievement;
        }

        if (killMimic == null)
            killMimic = lowestMimicsSlayed;

        if (killMimic != null)
        {
            Plugin.Logger.Msg($"Kill Mimic achievement: name='{killMimic.name}' target={killMimic.target} unlocked={killMimic.unlocked}");
            return;
        }

        Plugin.Logger.Warning("Kill Mimic achievement not found, Mimics will not be opened on purpose");
        LogMimicAchievementCandidates();
    }

    private void LogMimicAchievementCandidates()
    {
        Plugin.Logger.Msg($"{_playerInventory.achievements.Length} achievements loaded. Candidates:");
        foreach (Achievement achievement in _playerInventory.achievements)
        {
            string name = achievement.name ?? "";
            bool candidate = achievement.achievementType == AchievementType.MimicsSlayed
                || name.Contains("mimic", StringComparison.OrdinalIgnoreCase)
                || name.Contains("fun", StringComparison.OrdinalIgnoreCase)
                || name.Contains("chest", StringComparison.OrdinalIgnoreCase);
            if (!candidate) continue;

            Plugin.Logger.Msg($"  name='{achievement.name}' type={achievement.achievementType} target={achievement.target} unlocked={achievement.unlocked} localizedName='{achievement.localizedName}'");
        }
    }

    private void LateUpdate()
    {
        if (Input.GetKeyDown(Plugin.Settings.ArmoryChestMultiplyToggleKey.Value))
        {
            Plugin.Settings.MultiplyArmoryChests.Value = ToggleSetting("Armory Chest Multiplying", ref MultiplyArmoryChests);
        }


#if DEBUG
        DebugMode = true;
        if (Input.GetKeyDown(KeyCode.O) && !GameState.IsChestHunt())
        {
            _chestHuntManager.StartEvent();
        }

#endif
    }

    private void Update()
    {
        if (!GameState.IsChestHunt())
        {
            _chestOpenerRunning = false;
            if (!_achievementSearchDone)
                TryFindKillMimicAchievement();
            if (killMimic != null)
                killMimicCompleted = killMimic.unlocked;
        }
        else if (PerfectChestHuntOn)
        {
            if (_chestOpenerRunning) return;

            _chestOpenerRunning = true;

            MelonCoroutines.Start(RunPerfectChestHuntSequence());        
        }

        else
        {
            // open chests randomly
            if (!_chestOpenerRunning)
            {
                _chestOpenerRunning = true;

                MelonCoroutines.Start(RunRandomChestHuntSequence());
            }
        }
    }

    //////////////////////////////////////////////////////////////// Sequences
    
    
    private IEnumerator RunPerfectChestHuntSequence()
    {
        yield return WaitForChestHuntInitialise();

        if (!killMimicCompleted)
            yield return (OpenMimicChest());
        else
            yield return (OpenNormalChestsPerfectly());

        yield return WaitForPerfectChestsReady();
        OpenOnePerfectChest();

        yield return WaitThenClose();
    }

    private IEnumerator RunRandomChestHuntSequence()
    {
        yield return WaitForChestHuntInitialise();

        yield return OpenNormalChestsRandomly();
        yield return WaitForRandomOpenOutcome();

    }
    private IEnumerator WaitForRandomOpenOutcome()
    {
        while (_chestHuntManager.eventInProgress)
            yield return null;

        while (!IsCloseButtonActive() && !PerfectChestsHaveLoaded())
            yield return null;

        if (PerfectChestsHaveLoaded())
        {
            yield return WaitForPerfectChestsReady();
            OpenOnePerfectChest();
        }

        yield return WaitThenClose();

    }

    private IEnumerator WaitForChestHuntInitialise()
    {
        while (!_chestHuntManager.IsVisible() || _chestHuntManager.chests.Count == 0)
            yield return null; // Hold until chests are ready
    }
    private IEnumerator WaitForPerfectChestsReady()
    {
        while (!PerfectChestsHaveLoaded())
            yield return null;

        while (_chestHuntManager.perfectChests[2].perfectChestObject == null)
            yield return null;
    }

    private IEnumerator WaitThenClose()
    {
        while (!IsCloseButtonActive())
            yield return null;

        _chestHuntManager.Close();
    }


    //////////////////////////////////////////////////////////////// Open Procedures


    private IEnumerator OpenNormalChestsRandomly()
    {

        var shuffledChests = new List<Chest>(_chestHuntManager.chests.ToArray());

        ShuffleChests(shuffledChests);

        bool lastChestWasDupe = false;

        foreach (var chest in shuffledChests)
        {
            if (!chest.chestObject) continue;
            if (chest.opened) continue;
            if (DebugMode)
                Plugin.Logger.Msg($"Processing chest of type: {chest.type}");

            bool slowOpen = chest.type == ChestType.Mimic
             || (chest.type == ChestType.DuplicateNextPick && lastChestWasDupe);

            yield return OpenChest(chest.chestObject, slowOpen ? 3f : defaultChestOpeningDelay);

            lastChestWasDupe = chest.type == ChestType.DuplicateNextPick;

            if (!_chestHuntManager.eventInProgress) break;

        }
    }

    // Dupes first so their bonus lands on the next pick: Armory chests if maximised, otherwise the Multiplier Increaser
    // (or the best Multiplier when there is none). The Increaser then boosts the Multipliers opened after it.
    private IEnumerator OpenNormalChestsPerfectly()
    {
        yield return OpenDupeChests();

        if (Plugin.Settings.MultiplyArmoryChests.Value)
            yield return OpenAllChestsOfType(ChestType.ArmoryChest, defaultChestOpeningDelay);

        yield return OpenAllChestsOfType(ChestType.MultiplierIncreaser, defaultChestOpeningDelay);

        var bestChest = GetBestMultiplierChest();

        if (bestChest != null && !bestChest.opened)
        {
            if (DebugMode) Plugin.Logger.Msg($"Processing best Multiplier chest: {bestChest.multiplier}");
            yield return OpenChest(bestChest.chestObject, defaultChestOpeningDelay);
        }

        yield return OpenAllChestsOfType(ChestType.Multiplier, defaultChestOpeningDelay);
        yield return OpenAllChestsOfType(ChestType.Both, defaultChestOpeningDelay);
        yield return OpenAllChestsOfType(ChestType.CPS, defaultChestOpeningDelay);
        yield return OpenAllChestsOfType(ChestType.SlayerPoints, defaultChestOpeningDelay);
        yield return OpenAllChestsOfType(ChestType.Mimic, defaultChestOpeningDelay, exclude: true);
    }

    private IEnumerator OpenMimicChest()
    {
        int mimicsOpened = 0;

        foreach (var chest in _chestHuntManager.chests)
        {
            if (!chest.chestObject) continue;
            if (chest.type != ChestType.Mimic) continue;
            if (chest.opened) continue;
            if (DebugMode) Plugin.Logger.Msg($"Processing chest of type: {chest.type}");

            yield return OpenChest(chest.chestObject, defaultChestOpeningDelay);
            mimicsOpened++;

            if (mimicsOpened >= 3) break;
        }
    }

    private void OpenOnePerfectChest()
    {
        if (TryOpenPermanentUpgradeChest()) return;
        OpenBestMultiplierPerfectChest();
    }

    //////////////////////////////////////////////////////////////// Open Tools

    private Chest GetBestMultiplierChest()
    {
        Chest best = null;
        foreach (var chest in _chestHuntManager.chests)
        {
            if (chest.type != ChestType.Multiplier) continue;
            if (best == null || chest.multiplier > best.multiplier)
                best = chest;
        }
        return best;
    }

    private IEnumerator OpenDupeChests()
    {
        bool dupeOpened = false;
        foreach (var chest in _chestHuntManager.chests)
        {
            if (!chest.chestObject) continue;
            if (chest.opened) continue;
            if (chest.type != ChestType.DuplicateNextPick) continue;
            if (DebugMode) Plugin.Logger.Msg($"Processing chest of type: {chest.type}");

            yield return OpenChest(chest.chestObject, dupeOpened ? 3f : defaultChestOpeningDelay);
            dupeOpened = true;
        }
    }

    private IEnumerator OpenAllChestsOfType(ChestType type, float delay, bool exclude = false)
    {
        foreach (var chest in _chestHuntManager.chests)
        {
            if (!chest.chestObject) continue;
            if (chest.opened) continue;
            if (exclude ? chest.type == type : chest.type != type) continue;
            if (DebugMode) Plugin.Logger.Msg(chest.type == ChestType.Multiplier
                ? $"Processing chest of type: {chest.type} (multiplier {chest.multiplier})"
                : $"Processing chest of type: {chest.type}");
            yield return OpenChest(chest.chestObject, delay);
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

    private bool TryOpenPermanentUpgradeChest()
    {
        foreach (var chest in _chestHuntManager.perfectChests)
        {
            if (chest.type != ChestType.PermanentUpgrade) continue;
            if (!chest.perfectChestObject) continue;

            if (DebugMode) Plugin.Logger.Msg("Opening permanent upgrade chest...");
            chest.perfectChestObject?.Open(true);
            return true;
        }
        return false;
    }

    private void OpenBestMultiplierPerfectChest()
    {
        Chest best = null;
        foreach (var chest in _chestHuntManager.perfectChests)
        {
            if (best == null || chest.multiplier > best.multiplier)
                best = chest;
        }

        if (best == null) return;

        if (DebugMode) Plugin.Logger.Msg($"Highest multiplier chest found with multiplier: {best.multiplier}");

        best.perfectChestObject?.Open(true);
    }

    //////////////////////////////////////////////////////////////// Utilities

    private bool IsCloseButtonActive()
    {
        GameObject cb = _chestHuntManager.closeButton;
        if (cb == null) return false;

        AnimatedButton animatedButton = cb.GetComponent<AnimatedButton>();
        return animatedButton != null && animatedButton.isActiveAndEnabled;
    }

    private bool PerfectChestsHaveLoaded()
    {
        return _chestHuntManager.perfectChests != null
            && _chestHuntManager.perfectChests.Count > 0;
    }

    public void ShuffleChests(List<Chest> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private bool ToggleSetting(string type, ref bool state)
    {
        state = !state;

        Plugin.Logger.Msg($"{type} are: {(state ? "ON" : "OFF")}");
        Plugin.ModHelperInstance.ShowNotification(state ? $"{type} enabled!" : $"{type} disabled!", state);

        return state;
    }
}