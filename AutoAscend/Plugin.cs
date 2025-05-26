using IdleSlayerMods.Common;
using MelonLoader;
using System.Diagnostics;
using MyPluginInfo = AutoAscendMod.MyPluginInfo;
using Plugin = AutoAscendMod.Plugin;

[assembly: MelonInfo(typeof(Plugin), MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION, MyPluginInfo.PLUGIN_AUTHOR)]
[assembly: MelonAdditionalDependencies("IdleSlayerMods.Common")]

namespace AutoAscendMod;

public class Plugin : MelonMod
{
    internal static ConfigFile Config;
    internal static ModHelper ModHelperInstance;
    internal static readonly MelonLogger.Instance Logger = Melon<Plugin>.Logger;

    [Conditional("DEBUG")]
    public static void DLog(string message)
    {
        Logger.Msg(message);
    }

    public override void OnInitializeMelon()
    {
        Config = new(MyPluginInfo.PLUGIN_GUID);
        Logger.Msg($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        ModHelper.ModHelperMounted += SetModHelperInstance;

        var harmony = new HarmonyLib.Harmony(MyPluginInfo.PLUGIN_NAME);
        harmony.PatchAll();
    }
    private static void SetModHelperInstance(ModHelper instance) => ModHelperInstance = instance;

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName != "Game") return;
        ModUtils.RegisterComponent<AutoAscend>();
    }
}
