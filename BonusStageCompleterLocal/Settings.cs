using IdleSlayerMods.Common.Config;
using MelonLoader;
using UnityEngine;

namespace BonusStageCompleterLocal;

internal sealed class Settings(string configName) : BaseConfig(configName)
{
    internal MelonPreferences_Entry<KeyCode> ToggleKey;
    internal MelonPreferences_Entry<bool> EnableSkipAtSpiritBoost;
    internal MelonPreferences_Entry<bool> ShowPopUpSkipAtSpiritBoost;
    protected override void SetBindings()
    {
        EnableSkipAtSpiritBoost = Bind("Skip At Spirit Boost", "EnableSkipAtSpiritBoost", true,
            "Enable skip at spirit boost appeared bonus stage");
        ToggleKey = Bind("Skip At Spirit Boost", "ToggleKey", KeyCode.J,
            "The key bind for toggling 'skip at spirit boost appeared boosting'");
        ShowPopUpSkipAtSpiritBoost = Bind("Skip At Spirit Boost", "ShowPopUpSkipAtSpiritBoost", true,
            "Show a message popup to indicate whether 'skip at spirit boost has been toggled'.");

    }
}