using BepInEx.Configuration;
using IdleSlayerMods.Common.Config;
using UnityEngine;

namespace QualityOfLifeMods;

internal sealed class Settings(ConfigFile cfg) : BaseConfig(cfg)
{
    internal ConfigEntry<KeyCode> ChestPurchaseToggleKey;
    internal ConfigEntry<KeyCode> MinionSenderToggleKey;
    internal ConfigEntry<KeyCode> AddDragonScaleKey;
    internal ConfigEntry<bool> ChestPurchaseShowPopup;
    internal ConfigEntry<bool> MinionSenderShowPopup;
    protected override void SetBindings()
    {
        ChestPurchaseToggleKey = Bind("Chest Purchaser", "ChestPurchaseToggleKey", KeyCode.C,
            "The key bind for toggling Chest Purchaser mode");
        MinionSenderToggleKey = Bind("Chest Purchaser", "MinionSenderToggleKey", KeyCode.M,
            "The key bind for sending Minions");
        AddDragonScaleKey = Bind("Deagon Scale Adder", "AddDragonScaleKey", KeyCode.Q,
            "The key bind for adding Souls");
        ChestPurchaseShowPopup = Bind("Chest Purchaser", "ChestPurchaseShowPopup", true,
            "Show a message popup to indicate whether Chest Purchaser has been toggled.");
        MinionSenderShowPopup = Bind("Chest Purchaser", "MinionSenderShowPopup", true,
            "Show a message popup to indicate whether Minion Sender has been toggled.");
    }
}