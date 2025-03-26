using MelonLoader;
using IdleSlayerMods.Common.Config;
using UnityEngine;


namespace KillMimics;

internal sealed class Settings(string configName) : BaseConfig(configName)
{
    internal MelonPreferences_Entry<bool> ShouldRevealMultipliers;
    internal MelonPreferences_Entry<bool> ShouldRevealDuplicator;
    internal MelonPreferences_Entry<bool> ShouldRevealArmoryChest;

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