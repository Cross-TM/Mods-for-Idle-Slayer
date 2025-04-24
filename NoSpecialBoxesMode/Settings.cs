using HarmonyLib;
using IdleSlayerMods.Common;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using MyPluginInfo = NoSpecialBoxesMode.MyPluginInfo;
using Plugin = NoSpecialBoxesMode.Plugin;
using Il2Cpp;

[assembly: MelonInfo(typeof(Plugin), MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION, MyPluginInfo.PLUGIN_AUTHOR)]
[assembly: MelonAdditionalDependencies("IdleSlayerMods.Common")]

namespace NoSpecialBoxesMode;

public class Plugin : MelonMod
{
    internal static Settings Settings;
    internal static ModHelper ModHelperInstance;
    internal static readonly MelonLogger.Instance Logger = Melon<Plugin>.Logger;

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
        ModUtils.RegisterComponent<NoSpecialBoxes>();
    }
}