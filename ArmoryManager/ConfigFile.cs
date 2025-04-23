using IdleSlayerMods.Common.Config;
using MelonLoader;

namespace ArmoryManager;

internal sealed class ConfigFile(string configName) : BaseConfig(configName)
{
    internal MelonPreferences_Entry<int> NumberOfWeapons;

    protected override void SetBindings()
    {
        NumberOfWeapons = Bind("ArmoryManager", 15,
        "Number of weapons to keep in Armory");
    }
}
