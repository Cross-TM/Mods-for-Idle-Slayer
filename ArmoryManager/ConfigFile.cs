using IdleSlayerMods.Common.Config;
using MelonLoader;

namespace ArmoryManager;

internal sealed class ConfigFile(string configName) : BaseConfig(configName)
{
    internal MelonPreferences_Entry<int> LowerBoundOfWeapons;
    internal MelonPreferences_Entry<int> CurrentNumberOfWeapons;

    protected override void SetBindings()
    {
        LowerBoundOfWeapons = Bind("ArmoryManager", 15,
        "Lowest number of weapons to keep in Armory");
        CurrentNumberOfWeapons = Bind("ArmoryManager", 15,
            "Current number of weapons in Armory");
    }
}