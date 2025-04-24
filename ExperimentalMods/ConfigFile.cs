using IdleSlayerMods.Common.Config;
using MelonLoader;
using UnityEngine;

namespace ExperimentalMods;

internal sealed class ConfigFile(string configName) : BaseConfig(configName)
{
    internal MelonPreferences_Entry<KeyCode> AscendToggleKey;
    internal MelonPreferences_Entry<KeyCode> AscendToggleKey2;
    internal MelonPreferences_Entry<bool> AscendShowPopup;

    protected override void SetBindings()
    {
        AscendToggleKey = Bind("Auto Ascend Mode", "AscendToggleKey", KeyCode.Z,
            "The key bind for toggling auto rage mode");
        AscendToggleKey2 = Bind("Auto Ascend Mode", "AscendToggleKey2", KeyCode.X,
            "The key bind for toggling auto rage mode");
        AscendShowPopup = Bind("Auto Ascend Mode","AscendShowPopup", true,
            "Show a message popup to indicate whether auto Rage Mode has been toggled.");

    }
}