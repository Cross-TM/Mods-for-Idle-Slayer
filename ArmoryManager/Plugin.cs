using HarmonyLib;
using IdleSlayerMods.Common;
using Il2Cpp;
using MelonLoader;
using MelonLoader.Utils;
using System.Globalization;
using System.Security.Cryptography;
using MyPluginInfo = ArmoryManager.MyPluginInfo;
using Plugin = ArmoryManager.Plugin;

[assembly: MelonInfo(typeof(Plugin), MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION, MyPluginInfo.PLUGIN_AUTHOR)]
[assembly: MelonAdditionalDependencies("IdleSlayerMods.Common")]

namespace ArmoryManager;

public sealed class Plugin : MelonMod
{
    private static readonly string CloudRecoveryBlockPath = Path.Combine(
        MelonEnvironment.UserDataDirectory,
        "ArmoryManagerCloudRecovery.block");

    internal static ConfigFile Config;
    internal static ModHelper ModHelperInstance;
    internal static readonly MelonLogger.Instance Logger = Melon<Plugin>.Logger;
    internal static bool IsCloudRecoveryBlocked => File.Exists(CloudRecoveryBlockPath);

    public override void OnInitializeMelon()
    {
        ApplyPendingSaveRecovery();
        Config = new ConfigFile(MyPluginInfo.PLUGIN_GUID);
        ModHelper.ModHelperMounted += OnModHelperMounted;
        new HarmonyLib.Harmony(MyPluginInfo.PLUGIN_GUID).PatchAll();
        if (IsCloudRecoveryBlocked)
            Logger.Warning("RECOVERY GUARD ACTIVE: remote cloud-save downloads are blocked for this session.");
        Logger.Msg($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} loaded.");
    }

    private static void ApplyPendingSaveRecovery()
    {
        var markerPath = Path.Combine(MelonEnvironment.UserDataDirectory, "ArmoryManagerRecovery.flag");
        if (!File.Exists(markerPath))
            return;

        try
        {
            var expectedHash = File.ReadAllText(markerPath).Trim();
            var savePath = FileBasedPrefs.GetSaveFilePath();
            var actualHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(savePath)));
            if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Error(
                    $"RECOVERY ABORTED: save hash is {actualHash}, expected {expectedHash}. No save data changed.");
                return;
            }

            var timestamp = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d)
                .ToString("0.00000", CultureInfo.InvariantCulture);
            FileBasedPrefs.SetString("Last Played Date", timestamp);
            FileBasedPrefs.ManualySave(false);

            var savedTimestamp = FileBasedPrefs.GetString("Last Played Date", string.Empty);
            if (!string.Equals(timestamp, savedTimestamp, StringComparison.Ordinal))
                throw new InvalidOperationException("Idle Slayer did not retain the recovery timestamp.");

            File.Delete(markerPath);
            Logger.Warning(
                $"RECOVERY APPLIED: local pre-incident save timestamp raised to {timestamp}; " +
                "cloud download should no longer replace it.");
        }
        catch (Exception exception)
        {
            Logger.Error($"RECOVERY FAILED without deleting the marker: {exception}");
        }
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName == "Game")
            ModUtils.RegisterComponent<ArmoryController>();
    }

    private static void OnModHelperMounted(ModHelper instance)
    {
        ModHelperInstance = instance;
    }
}
