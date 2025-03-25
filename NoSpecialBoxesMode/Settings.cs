using BepInEx.Configuration;
using IdleSlayerMods.Common.Config;
using UnityEngine;

namespace NoSpecialBoxesMode;

internal sealed class Settings(ConfigFile cfg) : BaseConfig(cfg)
{
    internal ConfigEntry<KeyCode> SpecialBoxesToggleKey;
    internal ConfigEntry<KeyCode> BonusModeToggleKey;
    internal ConfigEntry<bool> SpecialBoxesShowPopup;
    internal ConfigEntry<bool> BonusModeShowPopup;

    protected override void SetBindings()
    {
        SpecialBoxesToggleKey = Bind("Special Boxes Mode", "SpecialBoxesToggleKey", KeyCode.S,
            "The key bind for toggling no Special Boxes Mode");
        BonusModeToggleKey = Bind("Bonus Mode Bypass", "BonusModeToggleKey", KeyCode.A,
            "The key bind for bypassing Bonus Mode Slider");
        SpecialBoxesShowPopup = Bind("Special Boxes Mode", "SpecialBoxesShowPopup", true,
            "Show a message popup to indicate whether no Special Boxes Mode has been toggled.");
        BonusModeShowPopup = Bind("Bonus Mode Bypass", "BonusModeShowPopup", true,
            "Show a message popup to indicate whether bypasing Bonus Mode Slider has been toggled.");
    }
}