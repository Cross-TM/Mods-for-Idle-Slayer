using MelonLoader;
using IdleSlayerMods.Common.Config;
using UnityEngine;

namespace MinionManaging;

internal sealed class Settings(string configName) : BaseConfig(configName)
{
    protected override void SetBindings()
    {
    }
}