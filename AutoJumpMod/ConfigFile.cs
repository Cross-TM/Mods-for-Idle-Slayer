using IdleSlayerMods.Common.Config;
using MelonLoader;
using UnityEngine;

namespace AutoJumpMod;

internal sealed class ConfigFile(string configName) : BaseConfig(configName)
{
    internal MelonPreferences_Entry<bool> UseAutoJump;
    internal MelonPreferences_Entry<KeyCode> AutoJumpToggleKey;
    internal MelonPreferences_Entry<bool> UseAutoBoost;
    internal MelonPreferences_Entry<KeyCode> AutoBoostToggleKey;
//    internal MelonPreferences_Entry<double> DelayBetweenArrows;
    protected override void SetBindings()
    {
        UseAutoJump = Bind("UseAutoJump", false,
            "Turn Auto Jump on and off");
        AutoJumpToggleKey = Bind("Auto Jump Toggle Key", "AutoJumpToggleKey", KeyCode.Z,
            "The key bind for toggling auto jump");
        UseAutoBoost = Bind("UseAutoBoost", false,
            "Turn Auto Boost on and off");
        AutoBoostToggleKey = Bind("Auto Boost Toggle Key", "AutoBoostToggleKey", KeyCode.B,
            "The key bind for toggling auto boost");
//        DelayBetweenArrows = Bind("Delay Between Arrows", 0.1,
//            "Time between shooting arrows");
    }
}
