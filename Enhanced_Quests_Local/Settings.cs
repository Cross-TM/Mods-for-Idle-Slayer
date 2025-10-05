using IdleSlayerMods.Common.Config;
using MelonLoader;

namespace Enhanced_Quests_Local;

internal sealed class Settings(string configName) : BaseConfig(configName)
{
    internal MelonPreferences_Entry<bool> ResetDailies;
    internal MelonPreferences_Entry<bool> ResetWeeklies;
    internal MelonPreferences_Entry<bool> ResetPortal;
    internal MelonPreferences_Entry<bool> ResetReroll;
    internal MelonPreferences_Entry<bool> EnableAutoReroll;
    internal MelonPreferences_Entry<bool> AutoRerollGetMaterials;
    internal MelonPreferences_Entry<bool> AutoRerollHitSilverBox;
    internal MelonPreferences_Entry<bool> AutoRerollHitRandomBox;
    internal MelonPreferences_Entry<int> SoftRerollCap;
    internal MelonPreferences_Entry<int> HardRerollCap;
    internal MelonPreferences_Entry<bool> EnableForceRerollTimer;
    internal MelonPreferences_Entry<int> RerollCheckMinutes;

    protected override void SetBindings()
    {
        ResetDailies = Bind("ResetDailies", true,
            "Toggle Reset Dailies");
        ResetWeeklies = Bind("ResetWeeklies", true,
            "Toggle Reset Weeklies");
        ResetPortal = Bind("ResetPortal", false,
            "Toggle Reset Portal");
        ResetReroll = Bind("ResetReroll", false,
            "Toggle Reset Quest Reroll");
        EnableAutoReroll = Bind("EnableAutoReroll", true,
            "Enable Automatic Quest Reroll Functionality");
        AutoRerollGetMaterials = Bind("AutoRerollGetMaterials", true,
            "Toggle Automatic Reroll of Get Materials Quests");
        AutoRerollHitSilverBox = Bind("AutoRerollHitSilverBox", true,
            "Toggle Automatic Reroll of Hit Silver Boxes Quests");
        AutoRerollHitRandomBox = Bind("AutoRerollHitRandomBox", false,
            "Toggle Automatic Reroll of Hit Random Boxes Quests");
        SoftRerollCap = Bind("SoftRerollCap", 15,
            "Soft reroll cap for medium quests (minimum: 15)");
        HardRerollCap = Bind("HardRerollCap", 20,
            "Hard reroll cap for long quests (minimum: 20)");
        EnableForceRerollTimer = Bind("EnableForceRerollTimer", false,
            "Toggle forced rerolls every X amount of minutes");
        RerollCheckMinutes = Bind("RerollCheckMinutes", 5,
            "Minutes between forced reroll checks (minimum: 5)");
    }
}