using IdleSlayerMods.Common.Config;
using MelonLoader;
using UnityEngine;

namespace AutoAscendingHeights;

internal sealed class ConfigFile(string configName) : BaseConfig(configName)
{
    internal MelonPreferences_Entry<KeyCode> AutoAscendingHeightsToggleKey;
    internal MelonPreferences_Entry<bool> AutoAscendingHeightsShowPopup;

    protected override void SetBindings()
    {
        AutoAscendingHeightsToggleKey = Bind("Auto Ascending Heights", "AutoAscendingHeightsToggleKey", KeyCode.Q,
                "The key bind for toggling auto Ascending Heights");
        AutoAscendingHeightsShowPopup = Bind("Auto Ascending Heights", "AutoAscendingHeightsShowPopup", true,
                "Show a message popup to indicate whether auto Ascending Heights has been toggled.");
    }
}