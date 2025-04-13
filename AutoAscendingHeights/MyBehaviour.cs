using IdleSlayerMods.Common.Extensions;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using static MelonLoader.MelonLogger;

namespace AutoAscendingHeights;

public class MyBehaviour : MonoBehaviour
{
    public static MyBehaviour Instance { get; private set; }
    private AscendingHeightsController _ascendingHeightsController;

    private void Awake()
    {
        Instance = this;

        _ascendingHeightsController = AscendingHeightsController.instance;

        if (_ascendingHeightsController == null)
        {
            Plugin.Logger.Msg("AscendingHeightsController instance is null!");
        }
        else
        {
            Plugin.Logger.Msg("AscendingHeightsController successfully found!");
        }

        _ascendingHeightsController.boostPlatformForce = 500f;
    }

    public void Start()
    {
        Plugin.Logger.Debug("MyBehaviour component initialized!");
        Plugin.Logger.Debug("Please delete these logs to keep the console clean!");
    }
}
