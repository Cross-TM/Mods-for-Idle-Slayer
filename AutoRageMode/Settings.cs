using MelonLoader;
using IdleSlayerMods.Common.Config;
using UnityEngine;

namespace AutoRageMode;

internal sealed class Settings(string configName) : BaseConfig(configName)
{
    internal MelonPreferences_Entry<KeyCode> RageToggleKey;
    internal MelonPreferences_Entry<KeyCode> HordeToggleKey;
    internal MelonPreferences_Entry<KeyCode> SoulsHordeToggleKey;

    internal MelonPreferences_Entry<bool> RageShowPopup;
    internal MelonPreferences_Entry<bool> HordeShowPopup;
    internal MelonPreferences_Entry<bool> SoulsHordeShowPopup;
    internal MelonPreferences_Entry<bool> DebugMode;

    protected override void SetBindings()
    {
        RageToggleKey = Bind("Auto Rage Mode", "RageToggleKey", KeyCode.T,
            "The key bind for toggling auto rage mode");
        RageShowPopup = Bind("Auto Rage Mode", "RageShowPopup", true,
            "Show a message popup to indicate whether auto Rage Mode has been toggled.");
        HordeToggleKey = Bind("Horde Only Mode", "HordeToggleKey", KeyCode.H,
            "The key bind for toggling Horde Mode only");
        HordeShowPopup = Bind("Horde Only Mode", "HordeShowPopup", true,
            "Show a message popup to indicate whether Horde Only Mode has been toggled.");
        SoulsHordeToggleKey = Bind("Souls Horde Only Mode", "SoulsHordeToggleKey", KeyCode.G,
            "The key bind for toggling Souls Horde Mode only");
        SoulsHordeShowPopup = Bind("Souls Horde Only Mode", "SoulsHordeShowPopup", true,
            "Show a message popup to indicate whether Souls Horde Only Mode has been toggled.");
        DebugMode = Bind("Debug Mode", "DebugMode", false,
            "Indicate whether Debug Mode is enabled.");


    }
}