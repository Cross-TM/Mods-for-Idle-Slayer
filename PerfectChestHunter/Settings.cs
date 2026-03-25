using MelonLoader;
using IdleSlayerMods.Common.Config;
using UnityEngine;

namespace PerfectChestHunter;

internal sealed class Settings(string configName) : BaseConfig(configName)
{
    internal MelonPreferences_Entry<KeyCode> ArmoryChestMultiplyToggleKey;
    internal MelonPreferences_Entry<bool> PerfectChestsHuntOn;
    internal MelonPreferences_Entry<bool> MultiplyArmoryChests;

    protected override void SetBindings()
    {
        ArmoryChestMultiplyToggleKey = Bind("Perfect Chest Hunter", "ArmoryChestMultiplyToggleKey ", KeyCode.C,
            "The key bind for toggling Armory Chest Maximums");
        PerfectChestsHuntOn = Bind("Perfect Chest Hunter", "PerfectChestsHuntOn", true,
            "Whether to open chests Perfectly or Randomly");
        MultiplyArmoryChests = Bind("Perfect Chest Hunter", "MultiplyArmoryChests", false,
            "Maximise the amount of Armory Chests to collect");
        
    }
}