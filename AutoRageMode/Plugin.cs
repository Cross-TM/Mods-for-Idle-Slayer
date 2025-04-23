using IdleSlayerMods.Common;
using Il2CppSystem.Runtime.Serialization.Formatters.Binary;
using MelonLoader;
using System.Diagnostics;
using MyPluginInfo = AutoRageMode.MyPluginInfo;
using Plugin = AutoRageMode.Plugin;

[assembly: MelonInfo(typeof(Plugin), MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION, MyPluginInfo.PLUGIN_AUTHOR)]
[assembly: MelonAdditionalDependencies("IdleSlayerMods.Common")]

namespace AutoRageMode;

public class Plugin : MelonMod
{
    internal static Settings Settings;
    internal static ModHelper ModHelperInstance;
    internal static readonly MelonLogger.Instance Logger = Melon<Plugin>.Logger;

    [Conditional("DEBUG")]
    public static void DLog(string message)
    {
        Logger.Msg(message);
    }

    public override void OnInitializeMelon()
    {
        Settings = new(MyPluginInfo.PLUGIN_GUID);
        LoggerInstance.Msg($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        ModHelper.ModHelperMounted += SetModHelperInstance;

        var harmony = new HarmonyLib.Harmony(MyPluginInfo.PLUGIN_NAME);
        harmony.PatchAll();
    }

    private static void SetModHelperInstance(ModHelper instance) => ModHelperInstance = instance;

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName != "Game") return;
        ModUtils.RegisterComponent<AutoRage>();
    }

}