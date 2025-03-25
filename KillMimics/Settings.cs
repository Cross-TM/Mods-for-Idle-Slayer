using BepInEx.Configuration;
using IdleSlayerMods.Common.Config;

namespace KillMimics;

internal sealed class Settings(ConfigFile cfg) : BaseConfig(cfg)
{
    internal ConfigEntry<bool> ShouldRevealMultipliers;
    internal ConfigEntry<bool> ShouldRevealDuplicator;
    internal ConfigEntry<bool> ShouldRevealArmoryChest;

    protected override void SetBindings()
    {
        ShouldRevealMultipliers = Bind("General", "ShouldRevealMultipliers", false,
            "Should reveal multipliers in chest hunt");
        ShouldRevealDuplicator = Bind("General", "ShouldRevealDuplicator", false,
            "Should reveal duplicator item in chest hunt");
        ShouldRevealArmoryChest = Bind("General", "ShouldRevealArmoryChest", false,
            "Should reveal armory chest in chest hunt");
    }
}