using IdleSlayerMods.Common;
using MelonLoader;
using MyPluginInfo = AutoAscendingHeights.MyPluginInfo;
using Plugin = AutoAscendingHeights.Plugin;

[assembly: MelonInfo(typeof(Plugin), MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION, MyPluginInfo.PLUGIN_AUTHOR)]
[assembly: MelonAdditionalDependencies("IdleSlayerMods.Common")]

namespace AutoAscendingHeights;

public class Plugin : MelonMod
{
    internal static ConfigFile Config;
    internal static ModHelper ModHelperInstance;

    internal static readonly MelonLogger.Instance Logger = Melon<Plugin>.Logger;

    public override void OnInitializeMelon()
    {
        Config = new(MyPluginInfo.PLUGIN_GUID);
        Logger.Msg($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        ModHelper.ModHelperMounted += SetModHelperInstance;

    }
    private static void SetModHelperInstance(ModHelper instance) => ModHelperInstance = instance;

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {

        if (sceneName != "Game") return;
        ModUtils.RegisterComponent<AutoAscendIngHeights>();
    }
}