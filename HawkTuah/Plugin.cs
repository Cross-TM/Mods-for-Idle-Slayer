using IdleSlayerMods.Common;
using MelonLoader;
using System.Reflection;
using MyPluginInfo = HawkTuah.MyPluginInfo;
using Plugin = HawkTuah.Plugin;

[assembly: MelonInfo(typeof(Plugin), MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION, MyPluginInfo.PLUGIN_AUTHOR)]
[assembly: MelonAdditionalDependencies("IdleSlayerMods.Common")]

namespace HawkTuah;

public class Plugin : MelonMod
{
    internal static readonly MelonLogger.Instance Logger = Melon<Plugin>.Logger;
    internal static ModHelper ModHelperInstance;

    public override void OnInitializeMelon()
    {
        Logger.Msg($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        ModHelper.ModHelperMounted += SetModHelperInstance;


    }
    private static void SetModHelperInstance(ModHelper instance) => ModHelperInstance = instance;

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        
        if (sceneName != "Game") return;
        ModUtils.RegisterComponent<HawkTuahRename>();
    }
}