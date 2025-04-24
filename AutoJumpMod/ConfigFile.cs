using IdleSlayerMods.Common.Config;
using MelonLoader;
using UnityEngine;

namespace AutoJumpMod;

internal sealed class ConfigFile(string configName) : BaseConfig(configName)
{
    internal MelonPreferences_Entry<bool> UseAutoJump;
    internal MelonPreferences_Entry<KeyCode> AutoJumpToggleKey;
//    internal MelonPreferences_Entry<double> DelayBetweenArrows;
    protected override void SetBindings()
    {
        UseAutoJump = Bind("UseAutoJump", false,
            "Turn Auto Jump on and off");
        AutoJumpToggleKey = Bind("Auto Jump Toggle Key", "AutoJumpToggleKey", KeyCode.Z,
            "The key bind for toggling auto jump");
//        DelayBetweenArrows = Bind("Delay Between Arrows", 0.1,
//            "Time between shooting arrows");
    }
}
