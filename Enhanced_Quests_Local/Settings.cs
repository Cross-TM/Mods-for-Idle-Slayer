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
    internal MelonPreferences_Entry<bool> AutoRerollWindDashKills;


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
        EnableAutoReroll = Bind("EnableAutoReroll", false,
            "Enable Automatic Quest Reroll Functionality");
        AutoRerollGetMaterials = Bind("AutoRerollGetMaterials", true,
            "Toggle Automatic Reroll of Get Materials Quests");
        AutoRerollWindDashKills = Bind("AutoRerollWindDashKills", true,
            "Toggle Automatic Reroll Wind Dash Kills");
        AutoRerollHitSilverBox = Bind("AutoRerollHitSilverBox", false,
            "Toggle Automatic Reroll of Hit Silver Boxes Quests");
        AutoRerollHitRandomBox = Bind("AutoRerollHitRandomBox", false,
            "Toggle Automatic Reroll of Hit Random Boxes Quests");

    }
}