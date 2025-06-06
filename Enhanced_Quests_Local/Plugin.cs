using IdleSlayerMods.Common;
using MelonLoader;
using MyPluginInfo = Enhanced_Quests_Local.MyPluginInfo;
using Plugin = Enhanced_Quests_Local.Plugin;

[assembly: MelonInfo(typeof(Plugin), MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION, MyPluginInfo.PLUGIN_AUTHOR)]
[assembly: MelonAdditionalDependencies("IdleSlayerMods.Common")]

namespace Enhanced_Quests_Local;

public class Plugin : MelonMod
{
    internal static readonly MelonLogger.Instance Logger = Melon<Plugin>.Logger;
    internal static Settings Settings;
    
    public override void OnInitializeMelon()
    {
        Settings = new(MyPluginInfo.PLUGIN_GUID);
        Logger.Msg($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName != "Game") return;
        ModUtils.RegisterComponent<Enhanced_Quests>();
    }
}
