using IdleSlayerMods.Common.Config;
using MelonLoader;

namespace BetterUpgrades;

internal sealed class Settings(string configName) : BaseConfig(configName)
{
    internal MelonPreferences_Entry<bool> RandomBoxMagnet;
    internal MelonPreferences_Entry<bool> SpecialRandomBoxMagnet;
    internal MelonPreferences_Entry<bool> DeltaWorms;

    protected override void SetBindings()
    {
        RandomBoxMagnet = Bind("Random Box Magnet", false,
            "Enable/Disable Random Box Upgrade");
        SpecialRandomBoxMagnet = Bind("Special Random Box Magnet", false,
            "Enable/Disable Special Random Box Upgrade");
        DeltaWorms = Bind("Delta Worms", false,
            "Enable/Disable Delta Worms Upgrade");
    }
}