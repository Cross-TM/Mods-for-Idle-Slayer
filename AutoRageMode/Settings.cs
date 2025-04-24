using MelonLoader;
using IdleSlayerMods.Common.Config;
using UnityEngine;

namespace AutoRageMode;

internal sealed class Settings(string configName) : BaseConfig(configName)
{
    internal MelonPreferences_Entry<KeyCode> RageToggleKey;
    internal MelonPreferences_Entry<bool> ShowPopups;
    internal MelonPreferences_Entry<int> CurrentMode;

    protected override void SetBindings()
    {
        RageToggleKey = Bind("Auto Rage Mode", "RageToggleKey", KeyCode.T,
            "The key bind for toggling auto rage mode");
        ShowPopups = Bind("Auto Rage Mode", "ShowPopups", true,
            "Show an on-screen notification whenever the mode changes");
        CurrentMode = Bind("General", "CurrentMode", (int)Mode.Off,
            "Last selected Rage Mode (0=Off, 1=SoulsHorde, 2=Horde, 3=Auto)");
    }
}