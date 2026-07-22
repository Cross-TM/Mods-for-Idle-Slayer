using ArmoryManager.Core;
using IdleSlayerMods.Common.Config;
using IdleSlayerMods.Common.Extensions;
using MelonLoader;
using UnityEngine;

namespace ArmoryManager;

internal sealed class ConfigFile : BaseConfig
{
    private readonly List<string> _obsoleteEntryIds = new();
    private string _lastInvalidGearList;

    internal ConfigFile(string configName) : base(configName)
    {
    }

    internal MelonPreferences_Entry<bool> AutomationEnabled;
    internal MelonPreferences_Entry<int> LowLevelMaximum;
    internal MelonPreferences_Entry<string> ExcludedGearTypes;
    internal MelonPreferences_Entry<int> MinimumItemsToKeep;
    internal MelonPreferences_Entry<int> RequirementPerLevel;
    internal string MigratedProtectedItemUids { get; private set; }

    protected override void SetBindings()
    {
        // Read all historical keys once so their useful values can be migrated, then delete them.
        Obsolete("Break Weapons Enabled", true);
        Obsolete("Break Dupe Weapons", true);
        Obsolete("Break Low Level Weapons", true);
        var oldRequirement = Obsolete("RequirementPerLevel", 15);
        var oldMinimum = Obsolete("LowerBoundOfWeapons", 5);
        Obsolete("Decrease Weapons To Keep", KeyCode.Minus);
        Obsolete("Increase Weapons To Keep", KeyCode.Equals);
        Obsolete("CurrentNumberOfWeaponsToKeep", 15);
        Obsolete("Config Schema Version", 0);
        Obsolete("Automation Enabled", false);
        Obsolete("Automatic Free Slots", true);
        Obsolete("Batch Notifications", true);
        var oldLowLevel = Obsolete("Low Level Maximum", 4);
        Obsolete("Manual Free Slots", 1);
        var oldProtected = Obsolete("Protected Item UIDs", string.Empty, null);
        Obsolete("Preview Key", KeyCode.F8);
        Obsolete("Execute Scan Key", KeyCode.F9);
        Obsolete("Decrease Free Slots Key", KeyCode.Minus);
        Obsolete("Increase Free Slots Key", KeyCode.Equals);
        Obsolete("Decrease Low Level Key", KeyCode.LeftBracket);
        Obsolete("Increase Low Level Key", KeyCode.RightBracket);
        Obsolete("Cleanup Swords", false);
        Obsolete("Cleanup Shields", false);
        Obsolete("Cleanup Armor", false);
        Obsolete("Cleanup Daggers", false);
        Obsolete("Cleanup Gloves", false);
        Obsolete("Cleanup Axes", false);
        Obsolete("Cleanup Capes", false);
        Obsolete("Cleanup Claws", false);
        var oldExcluded = Obsolete("Excluded Gear Types", "Armor", null);
        MigratedProtectedItemUids = oldProtected.Value ?? string.Empty;

        // These are the only entries that survive in ArmoryManager.cfg.
        AutomationEnabled = Configure(Bind("ArmoryManager", "Armory Manager Enabled", false),
            "MASTER SWITCH. Off means this mod never breaks any Armory item");
        LowLevelMaximum = Configure(
            Bind("ArmoryManager", "Break Items At Or Below Level", oldLowLevel.Value),
            "Main cleanup rule. 4 means eligible items at level 4 or lower are automatically broken");
        ExcludedGearTypes = Configure(
            Bind("ArmoryManager", "Never Auto-Break Gear Types", oldExcluded.Value ?? "Armor", null),
            "Comma-separated Armory types automation must never touch. Examples: Sword, Shield, Armor, Helmet, " +
            "Boots, Ring, Dagger, Axe, Staff, Bow, Spellbook. A typo disables cleanup for safety");
        MinimumItemsToKeep = Configure(
            Bind("ArmoryManager", "Advanced - Minimum Items To Keep", oldMinimum.Value),
            "Absolutely protect exactly these first physical storage positions from every cleanup rule, scanning " +
            "top-left, left-to-right, row-by-row. 20 protects slots 1-20: the first four rows of five slots");
        RequirementPerLevel = Configure(
            Bind("ArmoryManager", "Advanced - Equip Requirement Per Item Level", oldRequirement.Value),
            "Character levels required for each +1 Armory item level (15-50). This affects equipping, not cleanup");
    }

    internal void MigrateAndValidate(int maxSlots)
    {
        LowLevelMaximum.Value = Math.Clamp(LowLevelMaximum.Value, 0, 100);
        MinimumItemsToKeep.Value = Math.Clamp(MinimumItemsToKeep.Value, 0, Math.Max(0, maxSlots));
        RequirementPerLevel.Value = Math.Clamp(RequirementPerLevel.Value, 15, 50);

        AutomationEnabled.SaveEntry();
        LowLevelMaximum.SaveEntry();
        ExcludedGearTypes.SaveEntry();
        MinimumItemsToKeep.SaveEntry();
        RequirementPerLevel.SaveEntry();

        var category = AutomationEnabled.Category;
        foreach (var identifier in _obsoleteEntryIds)
            category.DeleteEntry(identifier);

        // Save after deletion so Mod Manager sees only the five streamlined entries.
        category.SaveToFile(false);
    }

    internal IReadOnlySet<string> GetEnabledGearTypes()
    {
        var excluded = new HashSet<string>(StringComparer.Ordinal);
        var rawValue = ExcludedGearTypes.Value ?? string.Empty;
        foreach (var value in rawValue.Split(
                     new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var normalized = GearTypes.NormalizeUserInput(value);
            if (normalized == null)
            {
                if (!string.Equals(_lastInvalidGearList, rawValue, StringComparison.Ordinal))
                {
                    Plugin.Logger.Error(
                        $"Invalid Never Auto-Break Gear Types value '{rawValue}'. Cleanup is disabled for safety.");
                    _lastInvalidGearList = rawValue;
                }

                return new HashSet<string>(StringComparer.Ordinal);
            }

            excluded.Add(normalized);
        }

        _lastInvalidGearList = null;
        return GearTypes.All.Where(type => !excluded.Contains(type)).ToHashSet(StringComparer.Ordinal);
    }

    private MelonPreferences_Entry<T> Obsolete<T>(
        string identifier,
        T defaultValue,
        string description = "")
    {
        var entry = Bind("ArmoryManager", identifier, defaultValue, description);
        entry.IsHidden = true;
        _obsoleteEntryIds.Add(identifier);
        return entry;
    }

    private static MelonPreferences_Entry<T> Configure<T>(
        MelonPreferences_Entry<T> entry,
        string description)
    {
        entry.Description = description;
        entry.IsHidden = false;
        return entry;
    }
}
