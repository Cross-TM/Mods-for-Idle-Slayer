using MelonLoader;
using IdleSlayerMods.Common.Config;
using UnityEngine;

namespace PerfectChestHunter;

internal sealed class Settings(string configName) : BaseConfig(configName)
{
    //    internal MelonPreferences_Entry<bool> ShouldRevealMultipliers;
    //    internal MelonPreferences_Entry<bool> ShouldRevealDuplicator;
    internal MelonPreferences_Entry<KeyCode> ArmoryChestMultiplyToggleKey;
    internal MelonPreferences_Entry<bool> MultiplyArmoryChests;

    protected override void SetBindings()
    {
        //ShouldRevealMultipliers = Bind("General", "ShouldRevealMultipliers", false,
        //            "Should reveal multipliers in chest hunt");
        //        ShouldRevealDuplicator = Bind("General", "ShouldRevealDuplicator", false,
        //            "Should reveal duplicator item in chest hunt");
        ArmoryChestMultiplyToggleKey = Bind("Perfect Chest Hunter", "ArmoryChestMultiplyToggleKey ", KeyCode.C,
                "The key bind for toggling Armory Chest Maximums");
        MultiplyArmoryChests = Bind("Perfect Chest Hunter", "MultiplyArmoryChests", false,
            "Maximise the amount of Armory Chests to collect");
        
    }
}