using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using IdleSlayerMods.Common;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace NoSpecialBoxesMode;

[BepInPlugin("com.CrossTM.NoSpecialBoxesMode", "No Special Boxes Mode", "1.0.0")]
[BepInDependency("IdleSlayerMods.Common")]
public class Plugin : BasePlugin
{
    internal new static ManualLogSource Log;
    internal static Settings Settings;
    internal static ModHelper ModHelperInstance;

    public override void Load()
    {
        Log = base.Log;
        Settings = new(Config);
        ClassInjector.RegisterTypeInIl2Cpp<NoSpecialBoxes>();

        ModHelper.ModHelperMounted += SetModHelperInstance;
        SceneManager.sceneLoaded += (UnityAction<Scene, LoadSceneMode>)OnSceneLoaded;
        Log.LogInfo($"No Special Boxes Mode Plugin is loaded!");

        var harmony = new Harmony("com.CrossTM.NoSpecialBoxesMode");
        Log.LogDebug("Applying Harmony Patches...");
        harmony.PatchAll();
    }

    private static void SetModHelperInstance(ModHelper instance) => ModHelperInstance = instance;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Game") return;
        AddComponent<NoSpecialBoxes>();
        SceneManager.sceneLoaded -= (UnityAction<Scene, LoadSceneMode>)OnSceneLoaded;
    }
}
