using BepInEx.Configuration;
using IdleSlayerMods.Common.Config;
using System.Numerics;
using UnityEngine;

namespace AutoRageMode;

internal sealed class Settings(ConfigFile cfg) : BaseConfig(cfg)
{
    internal ConfigEntry<KeyCode> RageToggleKey;
    internal ConfigEntry<KeyCode> HordeToggleKey;
    internal ConfigEntry<KeyCode> SoulsHordeToggleKey;

    internal ConfigEntry<bool> RageShowPopup;
    internal ConfigEntry<bool> HordeShowPopup;
    internal ConfigEntry<bool> SoulsHordeShowPopup;
    internal ConfigEntry<bool> DebugMode;

    protected override void SetBindings()
    {
        RageToggleKey = Bind("Auto Rage Mode", "RageToggleKey", KeyCode.R,
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