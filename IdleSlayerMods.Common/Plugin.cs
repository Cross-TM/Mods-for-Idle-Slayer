using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace IdleSlayerMods.Common;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    internal new static ManualLogSource Log;

    public override void Load()
    {
        Log = base.Log;
        
        SceneManager.sceneLoaded += (UnityAction<Scene, LoadSceneMode>)OnSceneLoaded;
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Game") return;
        AddComponent<ModHelper>();
        SceneManager.sceneLoaded -= (UnityAction<Scene, LoadSceneMode>)OnSceneLoaded;
    }
}