using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using IdleSlayerMods.Common;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace AutoRageMode;

[BepInPlugin("com.CrossTM.AutoRageMode", "Auto Rage Mode", "1.0.0")]
[BepInDependency("IdleSlayerMods.Common")]
// ReSharper disable once ClassNeverInstantiated.Global
public class Plugin : BasePlugin
{
    internal new static ManualLogSource Log;
    internal static Settings Settings;
    internal static ModHelper ModHelperInstance;

    public override void Load()
    {
        Log = base.Log;
        Settings = new(Config);
        ClassInjector.RegisterTypeInIl2Cpp<AutoRage>();

        ModHelper.ModHelperMounted += SetModHelperInstance;
        SceneManager.sceneLoaded += (UnityAction<Scene, LoadSceneMode>)OnSceneLoaded;
        Log.LogInfo($"Auto Rage Mode Plugin is loaded!");

        var harmony = new Harmony("com.CrossTM.AutoRageMode");
        harmony.PatchAll();
    }

    private static void SetModHelperInstance(ModHelper instance) => ModHelperInstance = instance;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Game") return;
        AddComponent<AutoRage>();
        SceneManager.sceneLoaded -= (UnityAction<Scene, LoadSceneMode>)OnSceneLoaded;
    }
}