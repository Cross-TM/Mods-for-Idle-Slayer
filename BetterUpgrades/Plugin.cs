using IdleSlayerMods.Common;
using MelonLoader;
using MyPluginInfo = BetterUpgrades.MyPluginInfo;
using Plugin = BetterUpgrades.Plugin;

[assembly: MelonInfo(typeof(Plugin), MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION, MyPluginInfo.PLUGIN_AUTHOR)]
[assembly: MelonAdditionalDependencies("IdleSlayerMods.Common")]

namespace BetterUpgrades;

public class Plugin : MelonMod
{
    internal static Settings Settings;
    internal static ModHelper ModHelperInstance;
    internal static readonly MelonLogger.Instance Logger = Melon<Plugin>.Logger;

    public override void OnInitializeMelon()
    {
        Settings = new(MyPluginInfo.PLUGIN_GUID);
        Logger.Msg($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        ModHelper.ModHelperMounted += SetModHelperInstance;
    }
    private static void SetModHelperInstance(ModHelper instance) => ModHelperInstance = instance;


    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName != "Game") return;
        ModUtils.RegisterComponent<DisableUpgrades>();
    }
}
