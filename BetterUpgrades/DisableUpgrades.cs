using Il2Cpp;
using UnityEngine;
using IdleSlayerMods.Common.Extensions;

namespace BetterUpgrades;

public class DisableUpgrades: MonoBehaviour
{
    public static DisableUpgrades Instance { get; private set; }

    private PlayerInventory _playerInventory;

    private readonly String RandomBoxMagnetName = "Random Box Vertical Magnet";
    private readonly String SpecialRandomBoxMagnetName = "Special Random Box Vertical Magnet";
    private readonly String DeltaWormsName = "Delta Worms";

    private static Boolean DisableRandomBoxMagnet => !Plugin.Settings.RandomBoxMagnet.Value;
    private static Boolean DisableSpecialRandomBoxMagnet => !Plugin.Settings.SpecialRandomBoxMagnet.Value;
    private static Boolean DisableDeltaWorms => !Plugin.Settings.DeltaWorms.Value;

    public void Awake()
    {
        Instance = this;

        _playerInventory = PlayerInventory.instance;

    }

    public void Start()
    {
        if (_playerInventory != null)
        {
            DisableUpgradesOnStart(RandomBoxMagnetName, DisableRandomBoxMagnet);

            DisableUpgradesOnStart(SpecialRandomBoxMagnetName, DisableSpecialRandomBoxMagnet);

            DisableUpgradesOnStart(DeltaWormsName, DisableDeltaWorms);
        }
    }
#if DEBUG

    public void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            LogUpgradeNamesAndDescriptions();        
        }
    }

        public void LogUpgradeNamesAndDescriptions()
        {
            var upgradesList = _playerInventory.upgrades;
            if (upgradesList == null)
            {
                Plugin.Logger.Msg("Upgrades list is null");
                return;
            }

            for (int i = 0; i < upgradesList.Length; i++)
            {
                Upgrade upgrade = upgradesList[i];
                if (upgrade == null) continue;

                Plugin.Logger.Msg($"Upgrade [{i}]: Name: {upgrade.localizedName}, Description: {upgrade.localizedDescription}");
            }
        }
    
#endif 

    public void DisableUpgradesOnStart(String upgradeName, Boolean PluginSetting)
    {
        var upgradesList = _playerInventory.upgrades;
        if (upgradesList == null)
        {
            Plugin.Logger.Debug("Upgrades list is null");
            return;
        }

        Upgrade upgradeToDisable = null;

        foreach (Upgrade upgrade in upgradesList)
        { 
            if (upgrade == null) continue;
            if (upgrade.localizedName == upgradeName)
            {
                upgradeToDisable = upgrade;
                break;
            }
        }

        if (PluginSetting && upgradeToDisable != null)
        {
            upgradeToDisable.disabled = true;

            Plugin.Logger.Msg($"Upgrade {upgradeToDisable.localizedName} disabled");
        }
    }

/*
    [HarmonyPatch(typeof(ShopManager), "OpenUpgrades")]
    public class UpgradePanelOpened
    {
        [HarmonyPostfix]
        static void UpgradesVisible()
        {
            //DisableUpgrades.Instance?.TriggerUpgradeDisable();
        }
    }

    [HarmonyPatch(typeof(ShopManager), "BuyAllUpgrades")]
    public class AllUpgradesPurchased
    {
        [HarmonyPostfix]
        static void UpgradesPurchased()
        {
            //DisableUpgrades.Instance?.TriggerUpgradeDisable();
        }
    }*/
}
