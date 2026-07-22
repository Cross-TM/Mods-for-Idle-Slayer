using IdleSlayerMods.Common.Config;
using MelonLoader;
using UnityEngine;

namespace AutoRageMode;

internal sealed class Settings(string configName) : BaseConfig(configName)
{
    internal MelonPreferences_Entry<KeyCode> RageToggleKey;
    internal MelonPreferences_Entry<bool> ShowPopups;
    internal MelonPreferences_Entry<int> CurrentMode;
    internal MelonPreferences_Entry<bool> ResetRageOnMegaHorde;
    internal MelonPreferences_Entry<bool> ResetRageAndActivateItemsOnMegaHorde;

    protected override void SetBindings()
    {
        RageToggleKey = Bind("Auto Rage Mode", "RageToggleKey", KeyCode.T,
            "The key bind for cycling through the auto rage modes");
        ShowPopups = Bind("Auto Rage Mode", "ShowPopups", true,
            "Show an on-screen notification whenever the mode changes");
        CurrentMode = Bind("General", "CurrentMode", (int)Mode.Off,
            "Last selected Rage Mode (0=Off, 1=SoulsHorde, 2=Horde, 3=Auto)");

        ResetRageOnMegaHorde = Bind("Mega Horde", "ResetRageAndActivate", false,
            "When a Mega Horde starts, reset the Rage Mode cooldown and activate Rage Mode");
        ResetRageAndActivateItemsOnMegaHorde = Bind("Mega Horde", "ResetRageActivateWithItems", false,
            "When a Mega Horde starts, reset and activate Rage Mode plus activate Shards Necklace and Souls Compass");
    }
}
