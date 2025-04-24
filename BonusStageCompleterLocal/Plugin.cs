using IdleSlayerMods.Common;
using MelonLoader;
using MyPluginInfo = BonusStageCompleterLocal.MyPluginInfo;
using Plugin = BonusStageCompleterLocal.Plugin;

[assembly: MelonInfo(typeof(Plugin), MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION, MyPluginInfo.PLUGIN_AUTHOR)]
[assembly: MelonAdditionalDependencies("IdleSlayerMods.Common")]

namespace BonusStageCompleterLocal;

public class Plugin : MelonMod
{
    internal static Settings Settings;
    internal static ModHelper ModHelperInstance;

    public override void OnInitializeMelon()
    {
        ModHelper.ModHelperMounted += SetModHelperInstance;
        Settings = new(MyPluginInfo.PLUGIN_GUID);
        LoggerInstance.Msg($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private static void SetModHelperInstance(ModHelper instance) => ModHelperInstance = instance;


    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName != "Game") return;
        ModUtils.RegisterComponent<BonusStageCompleter>();
    }
}