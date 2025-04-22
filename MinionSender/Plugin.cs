using HarmonyLib;
using IdleSlayerMods.Common;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using MyPluginInfo = MinionManaging.MyPluginInfo;
using Plugin = MinionManaging.Plugin;

[assembly: MelonInfo(typeof(Plugin), MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION, MyPluginInfo.PLUGIN_AUTHOR)]
[assembly: MelonAdditionalDependencies("IdleSlayerMods.Common")]

namespace MinionManaging;

public class Plugin : MelonMod
{
    internal static readonly MelonLogger.Instance Logger = Melon<Plugin>.Logger;
    internal static ModHelper ModHelperInstance;

    public override void OnInitializeMelon()
    {
        ClassInjector.RegisterTypeInIl2Cpp<MinionSender>();

        LoggerInstance.Msg($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        ModHelper.ModHelperMounted += SetModHelperInstance;

        var harmony = new HarmonyLib.Harmony(MyPluginInfo.PLUGIN_NAME);
        harmony.PatchAll();
    }

    private static void SetModHelperInstance(ModHelper instance) => ModHelperInstance = instance;

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName != "Game") return;
        ModUtils.RegisterComponent<MinionSender>();
    }
}