using IdleSlayerMods.Common.Extensions;
using MelonLoader;
using UnityEngine;

namespace Betterupgrades;

public class MyBehaviour : MonoBehaviour
{
    public void Start()
    {
        Plugin.Logger.Debug("MyBehaviour component initialized!");
        Plugin.Logger.Debug($"My setting is {Plugin.Config.MySetting.Value}");
        Plugin.Logger.Debug("Please delete these logs to keep the console clean!");
    }
}
