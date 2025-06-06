using IdleSlayerMods.Common.Config;
using MelonLoader;

namespace ArmoryManager;

internal sealed class ConfigFile(string configName) : BaseConfig(configName)
{
    internal MelonPreferences_Entry<int> LowerBoundOfWeapons;
    internal MelonPreferences_Entry<int> CurrentNumberOfWeaponsToKeep;

    protected override void SetBindings()
    {
        LowerBoundOfWeapons = Bind("LowerBoundOfWeapons", 15,
        "Lowest number of weapons to keep in Armory");
        CurrentNumberOfWeaponsToKeep = Bind("CurrentNumberOfWeaponsToKeep", 15,
            "Current number of weapons in Armory");
    }
}