using BepInEx.Configuration;
using IdleSlayerMods.Common.Config;
using UnityEngine;

namespace AutoRageMode;

internal sealed class Settings(ConfigFile cfg) : BaseConfig(cfg)
{
    internal ConfigEntry<KeyCode> RageToggleKey;
    internal ConfigEntry<KeyCode> HordeToggleKey;
    internal ConfigEntry<bool> RageShowPopup;
    internal ConfigEntry<bool> HordeShowPopup;

    protected override void SetBindings()
    {
        RageToggleKey = Bind("Auto Rage Mode", "ToggleKey", KeyCode.R,
            "The key bind for toggling auto rage mode");
        RageShowPopup = Bind("Auto Rage Mode", "ShowPopup", true,
            "Show a message popup to indicate whether auto Rage Mode has been toggled.");
        HordeToggleKey = Bind("Horde Only Mode", "ToggleKey", KeyCode.H,
            "The key bind for toggling Horde Mode only");
        HordeShowPopup = Bind("Horde Only Mode", "ShowPopup", true,
            "Show a message popup to indicate whether Horde Only Mode has been toggled.");
    }
}