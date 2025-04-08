using MelonLoader;
using IdleSlayerMods.Common.Config;
using UnityEngine;

namespace QualityOfLifeMods;

internal sealed class Settings(string configName) : BaseConfig(configName)
{
    internal MelonPreferences_Entry<KeyCode> ChestPurchaseToggleKey;
    internal MelonPreferences_Entry<KeyCode> MinionSenderToggleKey;
    internal MelonPreferences_Entry<bool> ChestPurchaseShowPopup;
    internal MelonPreferences_Entry<bool> MinionSenderShowPopup;
    protected override void SetBindings()
    {
        ChestPurchaseToggleKey = Bind("Chest Purchaser", "ChestPurchaseToggleKey", KeyCode.C,
            "The key bind for toggling Chest Purchaser mode");
        MinionSenderToggleKey = Bind("Chest Purchaser", "MinionSenderToggleKey", KeyCode.M,
            "The key bind for sending Minions");
        ChestPurchaseShowPopup = Bind("Chest Purchaser", "ChestPurchaseShowPopup", true,
            "Show a message popup to indicate whether Chest Purchaser has been toggled.");
        MinionSenderShowPopup = Bind("Chest Purchaser", "MinionSenderShowPopup", true,
            "Show a message popup to indicate whether Minion Sender has been toggled.");
    }
}