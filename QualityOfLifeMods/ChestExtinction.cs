using UnityEngine;
using System.Reflection;
using MelonLoader;
using Il2Cpp;

namespace QualityOfLifeMods
{
    public class ChestExtinctionPurchaser : MonoBehaviour
    {
        public static ChestExtinctionPurchaser Instance { get; private set; }
        private bool _chestExtinctionPurchaserEnabled;
        public bool ChestExtinctionPurchaserEnabled
        {
            get { return _chestExtinctionPurchaserEnabled; }
            private set { _chestExtinctionPurchaserEnabled = value; }
        }
        private bool _chestExtinctionActive;

        public Divinity chestExtinction;
        public string ChestExtinctionID;


        private void Awake()
        {
            Instance = this;
            if (Debug.isDebugBuild)
                Melon<Plugin>.Logger.Msg("ChestExtinction Awake() called");

            chestExtinction = Divinities.list.ChestExtinction;
            ChestExtinctionID = GetDivinityID(chestExtinction);

            _chestExtinctionPurchaserEnabled = false;

        }
        private void Start()
        {
            if (Debug.isDebugBuild)
                Melon<Plugin>.Logger.Msg("Checking Chest Extinction state on game start");

            if (DivinitiesManager.instance != null)
            {
                if (Debug.isDebugBuild)
                    Melon<Plugin>.Logger.Msg("DivinitiesManager found.");
                _chestExtinctionActive = Divinities.list.CheckUnlocked(chestExtinction);
            }
            else
            {
                if (Debug.isDebugBuild)
                    Melon<Plugin>.Logger.Msg("DivinitiesManager instance is null!");
            }


        }
        private void LateUpdate()
        {
            if (Input.GetKeyDown(Plugin.Settings.ChestPurchaseToggleKey.Value))
            {
                ToggleChestExtinction("Chest Extinction Purchaser", ref _chestExtinctionPurchaserEnabled, Plugin.Settings.ChestPurchaseShowPopup.Value);
            }

        }

        private void AddMoreDragonScales(double amount)
        {
            // Retrieve the DragonScale drop from the global drops list.
            Drop dragonScale = Drops.list.DragonScale;
            if (dragonScale == null)
            {
                Melon<Plugin>.Logger.Msg("DragonScale drop not found in Drops.list!");
                return;
            }

            // Call the DropsManager method to add the specified quantity.
            // Parameters: quantity, drop, isIdle, addQuestProgress, sound, dontDouble, surpassMaxLimit, showNotification
            double newTotal = DropsManager.instance.AddDrop(amount, dragonScale, false, false, true, false, false, true);

            Melon<Plugin>.Logger.Msg($"Added {amount} DragonScales. New total: {newTotal}");
        }


        private void ToggleChestExtinction(string type, ref bool state, bool showPopup)
        {
            state = !state;
            Melon<Plugin>.Logger.Msg($"{type} is now: {(state ? "ON" : "OFF")}");

            if (showPopup)
                Plugin.ModHelperInstance.ShowNotification(state ? $"{type} activated!" : $"{type} deactivated!", state);

        }


        public void ActivateChestExtinction()
        {
            if (DivinitiesManager.instance != null)
            {
                if (Debug.isDebugBuild)
                    Melon<Plugin>.Logger.Msg("Activating Chest Extinction.");

                DivinitiesManager.instance.ActivateDivinity(ChestExtinctionID);
                _chestExtinctionActive = true;
            }
            else
            {
                if (Debug.isDebugBuild)
                    Melon<Plugin>.Logger.Msg("DivinitiesManager instance is null! Cannot activate Chest Extinction.");
            }
        }

        public void DeactivateChestExtinction()
        {
            if (DivinitiesManager.instance != null)
            {
                if (Debug.isDebugBuild)
                    Melon<Plugin>.Logger.Msg("Deactivating Chest Extinction divinity.");

                DivinitiesManager.instance.DeactivateDivinity(ChestExtinctionID);
                _chestExtinctionActive = false;
            }
            else
            {
                if (Debug.isDebugBuild)
                    Melon<Plugin>.Logger.Msg("DivinitiesManager instance is null! Cannot deactivate Chest Extinction.");
            }
        }
        public string GetDivinityID(Divinity divinity)
        {

            if (divinity == null)
            {
                if (Debug.isDebugBuild)
                    Melon<Plugin>.Logger.Msg("Divinity instance is null.");
                return "";
            }

            PropertyInfo idProperty = divinity.GetType().GetProperty("id");
            if (idProperty != null)
            {
                object idValue = idProperty.GetValue(divinity);
                return idValue.ToString();
            }
            else
            {
                return "";
            }
        }
    }
}