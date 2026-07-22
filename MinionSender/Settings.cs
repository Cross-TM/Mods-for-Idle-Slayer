using IdleSlayerMods.Common.Config;
using MelonLoader;

namespace MinionManaging;

internal sealed class Settings : BaseConfig
{
    internal Settings(string configName) : base(configName)
    {
    }

    internal MelonPreferences_Entry<bool> AutoPrestigeEnabled;
    internal MelonPreferences_Entry<bool> AutoResendAfterPrestige;
    internal MelonPreferences_Entry<bool> PrestigeActiveMissions;
    internal MelonPreferences_Entry<bool> ShowPrestigeNotifications;
    internal MelonPreferences_Entry<bool> AutoDailyBonusEnabled;
    internal MelonPreferences_Entry<int> DivinityPointsMultiplier;

    internal MelonPreferences_Entry<bool> PrestigeWhiteKnight;
    internal MelonPreferences_Entry<bool> PrestigeDarkWizard;
    internal MelonPreferences_Entry<bool> PrestigeStoneGolem;
    internal MelonPreferences_Entry<bool> PrestigeSkeleton;
    internal MelonPreferences_Entry<bool> PrestigeGooMutant;
    internal MelonPreferences_Entry<bool> PrestigeCyclops;
    internal MelonPreferences_Entry<bool> PrestigeNinja;

    protected override void SetBindings()
    {
        AutoPrestigeEnabled = Bind("Automatic Prestige", "AutoPrestigeEnabled", true,
            "Enable automatic prestige for explicitly selected minions.");
        AutoResendAfterPrestige = Bind("Automatic Prestige", "AutoResendAfterPrestige", true,
            "Send a minion on a new mission after automatic prestige.");
        PrestigeActiveMissions = Bind("Automatic Prestige", "PrestigeActiveMissions", false,
            "Allow automatic prestige to interrupt an active mission at max level. The pending mission reward is lost.");
        ShowPrestigeNotifications = Bind("Automatic Prestige", "ShowPrestigeNotifications", true,
            "Show short notifications for automatic prestige actions.");
        AutoDailyBonusEnabled = Bind("Daily Bonus", "AutoDailyBonusEnabled", true,
            "Automatically send available minions, claim the daily minion bonus, then claim and resend completed missions.");
        DivinityPointsMultiplier = Bind("Automatic Prestige", "DivinityPointsMultiplier", 1,
            "Multiply the normal Divinity Points reward from automatic prestige (minimum 1, maximum 100).");

        PrestigeWhiteKnight = Bind("Minions", "PrestigeWhiteKnight", false,
            "Allow automatic prestige for the White Knight.");
        PrestigeDarkWizard = Bind("Minions", "PrestigeDarkWizard", false,
            "Allow automatic prestige for the Dark Wizard.");
        PrestigeStoneGolem = Bind("Minions", "PrestigeStoneGolem", false,
            "Allow automatic prestige for the Stone Golem.");
        PrestigeSkeleton = Bind("Minions", "PrestigeSkeleton", false,
            "Allow automatic prestige for the Skeleton.");
        PrestigeGooMutant = Bind("Minions", "PrestigeGooMutant", false,
            "Allow automatic prestige for the Goo Mutant.");
        PrestigeCyclops = Bind("Minions", "PrestigeCyclops", false,
            "Allow automatic prestige for the Cyclops.");
        PrestigeNinja = Bind("Minions", "PrestigeNinja", false,
            "Allow automatic prestige for the Ninja.");
    }
}
