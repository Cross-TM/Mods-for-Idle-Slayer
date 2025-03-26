using UnityEngine;
using UnityEngine.SceneManagement;
using Il2CppInterop.Runtime.Injection;
using UnityEngine.Events;
using IdleSlayerMods.Common;
using MelonLoader;
using MyPluginInfo = KillMimics.MyPluginInfo;
using Plugin = KillMimics.Plugin;

[assembly: MelonInfo(typeof(Plugin), MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION, MyPluginInfo.PLUGIN_AUTHOR)]
[assembly: MelonAdditionalDependencies("IdleSlayerMods.Common")]

namespace KillMimics;

public class Plugin : MelonMod
{
    internal static Settings Settings;
    internal static ModHelper ModHelperInstance;

    public override void OnInitializeMelon()
    {
        ClassInjector.RegisterTypeInIl2Cpp<ChestKiller>();
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

        var chestHunt = GameObject.Find("Chest Hunt");
        if (chestHunt)
            chestHunt.AddComponent<ChestKiller>();
    }
}