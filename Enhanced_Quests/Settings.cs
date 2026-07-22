using IdleSlayerMods.Common.Config;
using Il2Cpp;
using MelonLoader;

namespace Enhanced_Quests;

internal sealed class Settings : BaseConfig
{
    private const string GeneralCategory = "Enhanced Quests";
    private const string DailyCategory = "Enhanced Quests - Daily";
    private const string WeeklyCategory = "Enhanced Quests - Weekly";

    private readonly string _configPath;
    private DateTime _lastConfigWriteUtc;

    internal Settings(string configName) : base(MigrateLegacyConfig(configName))
    {
        _configPath = GetConfigPath(configName);
        _lastConfigWriteUtc = File.Exists(_configPath) ? File.GetLastWriteTimeUtc(_configPath) : DateTime.MinValue;
    }

    internal MelonPreferences_Entry<bool> Enabled;
    internal MelonPreferences_Entry<bool> AutoClaimQuests;
    internal MelonPreferences_Entry<bool> AutoRerollDailyQuests;
    internal MelonPreferences_Entry<bool> AutoRerollWeeklyQuests;

    internal MelonPreferences_Entry<bool> DailyKillEnemies;
    internal MelonPreferences_Entry<bool> DailyPickUpCoins;
    internal MelonPreferences_Entry<bool> DailyHitRandomBoxes;
    internal MelonPreferences_Entry<bool> DailyHitRandomSilverBoxes;
    internal MelonPreferences_Entry<bool> DailyKillGiants;
    internal MelonPreferences_Entry<bool> DailyKillEnemiesWithRageMode;
    internal MelonPreferences_Entry<bool> DailyBoost;
    internal MelonPreferences_Entry<bool> DailyCraftTemporaryItems;
    internal MelonPreferences_Entry<bool> DailyChestHuntChests;
    internal MelonPreferences_Entry<bool> DailyBonusStage;
    internal MelonPreferences_Entry<bool> DailyBonusStageSections;
    internal MelonPreferences_Entry<bool> DailyGetMaterials;
    internal MelonPreferences_Entry<bool> DailyPickUpGemstones;
    internal MelonPreferences_Entry<bool> DailyWindDashKills;
    internal MelonPreferences_Entry<bool> DailyUseRageMode;
    internal MelonPreferences_Entry<bool> DailyCompleteAscendingHeights;

    internal MelonPreferences_Entry<bool> WeeklyKillEnemiesWithRageMode;
    internal MelonPreferences_Entry<bool> WeeklyChestHuntChests;
    internal MelonPreferences_Entry<bool> WeeklyGoThroughPortals;
    internal MelonPreferences_Entry<bool> WeeklyCompleteDailyQuests;

    protected override void SetBindings()
    {
        Enabled = Setting(GeneralCategory, "Enabled", "Enabled");
        AutoClaimQuests = Setting(GeneralCategory, "AutoClaim", "Auto Claim");
        AutoRerollDailyQuests = Setting(GeneralCategory, "RerollDaily", "Reroll Daily Quests");
        AutoRerollWeeklyQuests = Setting(GeneralCategory, "RerollWeekly", "Reroll Weekly Quests");

        DailyKillEnemies = DailyType("KillEnemies", "Kill Enemies");
        DailyPickUpCoins = DailyType("PickUpCoins", "Pick Up Coins");
        DailyHitRandomBoxes = DailyType("HitRandomBoxes", "Hit Random Boxes");
        DailyHitRandomSilverBoxes = DailyType("HitRandomSilverBoxes", "Hit Silver Random Boxes");
        DailyKillGiants = DailyType("KillGiants", "Kill Giants");
        DailyKillEnemiesWithRageMode = DailyType("KillEnemiesWithRageMode", "Rage Mode Kills");
        DailyBoost = DailyType("Boost", "Boost");
        DailyCraftTemporaryItems = DailyType("CraftTemporaryItems", "Craft Temporary Items");
        DailyChestHuntChests = DailyType("ChestHuntChests", "Chest Hunt Chests");
        DailyBonusStage = DailyType("BonusStage", "Bonus Stages");
        DailyBonusStageSections = DailyType("BonusStageSections", "Bonus Stage Sections");
        DailyGetMaterials = DailyType("GetMaterials", "Get Materials");
        DailyPickUpGemstones = DailyType("PickUpGemstones", "Pick Up Gemstones");
        DailyWindDashKills = DailyType("WindDashKills", "Wind Dash Kills");
        DailyUseRageMode = DailyType("UseRageMode", "Use Rage Mode");
        DailyCompleteAscendingHeights = DailyType("CompleteAscendingHeights", "Ascending Heights");

        WeeklyKillEnemiesWithRageMode = WeeklyType("KillEnemiesWithRageMode", "Rage Mode Kills");
        WeeklyChestHuntChests = WeeklyType("ChestHuntChests", "Chest Hunt Chests");
        WeeklyGoThroughPortals = WeeklyType("GoThroughPortals", "Go Through Portals");
        WeeklyCompleteDailyQuests = WeeklyType("CompleteDailyQuests", "Complete Daily Quests");
    }

    private MelonPreferences_Entry<bool> Setting(string category, string key, string displayName)
    {
        MelonPreferences_Entry<bool> entry = Bind(category, key, true, string.Empty);
        entry.DisplayName = displayName;
        return entry;
    }

    private MelonPreferences_Entry<bool> DailyType(string key, string displayName) =>
        Setting(DailyCategory, key, displayName);

    private MelonPreferences_Entry<bool> WeeklyType(string key, string displayName) =>
        Setting(WeeklyCategory, key, displayName);

    internal bool HasAllowedDailyTypes() =>
        DailyKillEnemies.Value || DailyPickUpCoins.Value || DailyHitRandomBoxes.Value ||
        DailyHitRandomSilverBoxes.Value || DailyKillGiants.Value || DailyKillEnemiesWithRageMode.Value ||
        DailyBoost.Value || DailyCraftTemporaryItems.Value || DailyChestHuntChests.Value ||
        DailyBonusStage.Value || DailyBonusStageSections.Value || DailyGetMaterials.Value ||
        DailyPickUpGemstones.Value || DailyWindDashKills.Value || DailyUseRageMode.Value ||
        DailyCompleteAscendingHeights.Value;

    internal bool HasAllowedWeeklyTypes() =>
        WeeklyKillEnemiesWithRageMode.Value || WeeklyChestHuntChests.Value ||
        WeeklyGoThroughPortals.Value || WeeklyCompleteDailyQuests.Value;

    internal bool TryGetDailyTypeAllowed(QuestType type, out bool allowed)
    {
        allowed = type switch
        {
            QuestType.KillEnemies => DailyKillEnemies.Value,
            QuestType.PickUpCoins => DailyPickUpCoins.Value,
            QuestType.HitRandomBoxes => DailyHitRandomBoxes.Value,
            QuestType.HitRandomSilverBoxes => DailyHitRandomSilverBoxes.Value,
            QuestType.KillGiants => DailyKillGiants.Value,
            QuestType.KillEnemiesWithRageMode => DailyKillEnemiesWithRageMode.Value,
            QuestType.Boost => DailyBoost.Value,
            QuestType.CraftTemporaryItems => DailyCraftTemporaryItems.Value,
            QuestType.ChestHuntChests => DailyChestHuntChests.Value,
            QuestType.BonusStage => DailyBonusStage.Value,
            QuestType.BonusStageSections => DailyBonusStageSections.Value,
            QuestType.GetMaterials => DailyGetMaterials.Value,
            QuestType.PickUpGemstones => DailyPickUpGemstones.Value,
            QuestType.WindDashKills => DailyWindDashKills.Value,
            QuestType.UseRageMode => DailyUseRageMode.Value,
            QuestType.CompleteAscendingHeights => DailyCompleteAscendingHeights.Value,
            _ => true
        };

        return type is QuestType.KillEnemies or QuestType.PickUpCoins or QuestType.HitRandomBoxes or
            QuestType.HitRandomSilverBoxes or QuestType.KillGiants or QuestType.KillEnemiesWithRageMode or
            QuestType.Boost or QuestType.CraftTemporaryItems or QuestType.ChestHuntChests or
            QuestType.BonusStage or QuestType.BonusStageSections or QuestType.GetMaterials or
            QuestType.PickUpGemstones or QuestType.WindDashKills or QuestType.UseRageMode or
            QuestType.CompleteAscendingHeights;
    }

    internal bool TryGetWeeklyTypeAllowed(QuestType type, out bool allowed)
    {
        allowed = type switch
        {
            QuestType.KillEnemiesWithRageMode => WeeklyKillEnemiesWithRageMode.Value,
            QuestType.ChestHuntChests => WeeklyChestHuntChests.Value,
            QuestType.GoThroughPortals => WeeklyGoThroughPortals.Value,
            QuestType.CompleteDailyQuests => WeeklyCompleteDailyQuests.Value,
            _ => true
        };

        return type is QuestType.KillEnemiesWithRageMode or QuestType.ChestHuntChests or
            QuestType.GoThroughPortals or QuestType.CompleteDailyQuests;
    }

    internal bool ReloadIfChanged()
    {
        try
        {
            if (!File.Exists(_configPath))
                return false;

            DateTime writeTimeUtc = File.GetLastWriteTimeUtc(_configPath);
            if (writeTimeUtc <= _lastConfigWriteUtc)
                return false;

            _lastConfigWriteUtc = writeTimeUtc;
            Dictionary<string, bool> values = ReadBooleanValues(File.ReadAllLines(_configPath));

            Apply(Enabled, GeneralCategory, "Enabled");
            Apply(AutoClaimQuests, GeneralCategory, "AutoClaim");
            Apply(AutoRerollDailyQuests, GeneralCategory, "RerollDaily");
            Apply(AutoRerollWeeklyQuests, GeneralCategory, "RerollWeekly");

            Apply(DailyKillEnemies, DailyCategory, "KillEnemies");
            Apply(DailyPickUpCoins, DailyCategory, "PickUpCoins");
            Apply(DailyHitRandomBoxes, DailyCategory, "HitRandomBoxes");
            Apply(DailyHitRandomSilverBoxes, DailyCategory, "HitRandomSilverBoxes");
            Apply(DailyKillGiants, DailyCategory, "KillGiants");
            Apply(DailyKillEnemiesWithRageMode, DailyCategory, "KillEnemiesWithRageMode");
            Apply(DailyBoost, DailyCategory, "Boost");
            Apply(DailyCraftTemporaryItems, DailyCategory, "CraftTemporaryItems");
            Apply(DailyChestHuntChests, DailyCategory, "ChestHuntChests");
            Apply(DailyBonusStage, DailyCategory, "BonusStage");
            Apply(DailyBonusStageSections, DailyCategory, "BonusStageSections");
            Apply(DailyGetMaterials, DailyCategory, "GetMaterials");
            Apply(DailyPickUpGemstones, DailyCategory, "PickUpGemstones");
            Apply(DailyWindDashKills, DailyCategory, "WindDashKills");
            Apply(DailyUseRageMode, DailyCategory, "UseRageMode");
            Apply(DailyCompleteAscendingHeights, DailyCategory, "CompleteAscendingHeights");

            Apply(WeeklyKillEnemiesWithRageMode, WeeklyCategory, "KillEnemiesWithRageMode");
            Apply(WeeklyChestHuntChests, WeeklyCategory, "ChestHuntChests");
            Apply(WeeklyGoThroughPortals, WeeklyCategory, "GoThroughPortals");
            Apply(WeeklyCompleteDailyQuests, WeeklyCategory, "CompleteDailyQuests");
            return true;

            void Apply(MelonPreferences_Entry<bool> entry, string category, string key)
            {
                if (entry == null || !values.TryGetValue($"{category}\n{key}", out bool value) || entry.Value == value)
                    return;

                entry.Value = value;
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.Warning($"Unable to reload quest settings: {ex.Message}");
            return false;
        }
    }

    private static string MigrateLegacyConfig(string configName)
    {
        try
        {
            string configPath = GetConfigPath(configName);
            if (!File.Exists(configPath))
                return configName;

            string[] lines = File.ReadAllLines(configPath);
            if (lines.Any(line => line.Trim().Equals($"[\"{DailyCategory}\"]", StringComparison.Ordinal)))
                return configName;

            Dictionary<string, bool> values = ReadBooleanValues(lines);
            bool Old(string section, string key, bool fallback = true) =>
                values.TryGetValue($"{section}\n{key}", out bool value) ? value : fallback;

            List<string> migrated = new()
            {
                $"[\"{GeneralCategory}\"]",
                $"Enabled = {Old("General", "Enabled").ToString().ToLowerInvariant()}",
                $"AutoClaim = {Old("General", "AutoClaimQuests").ToString().ToLowerInvariant()}",
                $"RerollDaily = {Old("General", "AutoRerollDailyQuests").ToString().ToLowerInvariant()}",
                $"RerollWeekly = {Old("General", "AutoRerollWeeklyQuests").ToString().ToLowerInvariant()}",
                string.Empty,
                $"[\"{DailyCategory}\"]"
            };

            string[] dailyTypes =
            {
                "KillEnemies", "PickUpCoins", "HitRandomBoxes", "HitRandomSilverBoxes", "KillGiants",
                "KillEnemiesWithRageMode", "Boost", "CraftTemporaryItems", "ChestHuntChests", "BonusStage",
                "BonusStageSections", "GetMaterials", "PickUpGemstones", "WindDashKills", "UseRageMode",
                "CompleteAscendingHeights"
            };
            migrated.AddRange(dailyTypes.Select(type =>
                $"{type} = {Old("Daily Quest Types", $"Accept{type}").ToString().ToLowerInvariant()}"));

            migrated.Add(string.Empty);
            migrated.Add($"[\"{WeeklyCategory}\"]");
            string[] weeklyTypes =
            {
                "KillEnemiesWithRageMode", "ChestHuntChests", "GoThroughPortals", "CompleteDailyQuests"
            };
            migrated.AddRange(weeklyTypes.Select(type =>
                $"{type} = {Old("Weekly Quest Types", $"Accept{type}").ToString().ToLowerInvariant()}"));

            File.WriteAllLines(configPath, migrated);
        }
        catch
        {
            // Configuration binding still works with defaults if migration is unavailable.
        }

        return configName;
    }

    private static string GetConfigPath(string configName) => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "IdleSlayerModManager", "ModLoader", "UserData", $"{configName}.cfg");

    private static Dictionary<string, bool> ReadBooleanValues(IEnumerable<string> lines)
    {
        Dictionary<string, bool> values = new(StringComparer.Ordinal);
        string section = string.Empty;
        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                section = line[1..^1].Trim().Trim('"');
                continue;
            }

            int equals = line.IndexOf('=');
            if (equals <= 0)
                continue;

            string key = line[..equals].Trim();
            if (bool.TryParse(line[(equals + 1)..].Trim(), out bool value))
                values[$"{section}\n{key}"] = value;
        }

        return values;
    }
}
