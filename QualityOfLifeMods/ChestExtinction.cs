using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using static UnityEngine.Random;

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
            Plugin.Log.LogDebug("ChestExtinction Awake() called");

            chestExtinction = Divinities.list.ChestExtinction;
            ChestExtinctionID = GetDivinityID(chestExtinction);

            _chestExtinctionPurchaserEnabled = false;

        }
        private void Start()
        {
            Plugin.Log.LogDebug("Checking Chest Extinction state on game start");

            if (DivinitiesManager.instance != null)
            {
                Plugin.Log.LogDebug("DivinitiesManager found.");
                _chestExtinctionActive = Divinities.list.CheckUnlocked(chestExtinction);
            }
            else
            {
                Plugin.Log.LogError("DivinitiesManager instance is null!");
            }

            ToggleChestExtinction("Chest Extinction Purchaser", ref _chestExtinctionPurchaserEnabled, Plugin.Settings.ChestPurchaseShowPopup.Value);

        }
        private void LateUpdate()
        {
            if (Input.GetKeyDown(Plugin.Settings.ChestPurchaseToggleKey.Value))
            {
                ToggleChestExtinction("Chest Extinction Purchaser", ref _chestExtinctionPurchaserEnabled, Plugin.Settings.ChestPurchaseShowPopup.Value);
            }

            if (Input.GetKeyDown(Plugin.Settings.AddDragonScaleKey.Value))
            {
                AddMoreDragonScales(5.0);
            }

        }

        private void AddMoreDragonScales(double amount)
        {
            // Retrieve the DragonScale drop from the global drops list.
            Drop dragonScale = Drops.list.DragonScale;
            if (dragonScale == null)
            {
                Plugin.Log.LogInfo("DragonScale drop not found in Drops.list!");
                return;
            }

            // Call the DropsManager method to add the specified quantity.
            // Parameters: quantity, drop, isIdle, addQuestProgress, sound, dontDouble, surpassMaxLimit, showNotification
            double newTotal = DropsManager.instance.AddDrop(amount, dragonScale, false, false, true, false, false, true);

            Plugin.Log.LogInfo($"Added {amount} DragonScales. New total: {newTotal}");
        }


        private void ToggleChestExtinction(string type, ref bool state, bool showPopup)
        {
            state = !state;
            Plugin.Log.LogInfo($"{type} is now: {(state ? "ON" : "OFF")}");

            if (showPopup)
                Plugin.ModHelperInstance.ShowNotification(state ? $"{type} activated!" : $"{type} deactivated!", state);

        }


        public void ActivateChestExtinction()
        {
            if (DivinitiesManager.instance != null)
            {
                Plugin.Log.LogDebug("Activating Chest Extinction.");

                DivinitiesManager.instance.ActivateDivinity(ChestExtinctionID);
                _chestExtinctionActive = true;
            }
            else
            {
                Plugin.Log.LogError("DivinitiesManager instance is null! Cannot activate Chest Extinction.");
            }
        }

        public void DeactivateChestExtinction()
        {
            if (DivinitiesManager.instance != null)
            {
                Plugin.Log.LogDebug("Deactivating Chest Extinction divinity.");

                DivinitiesManager.instance.DeactivateDivinity(ChestExtinctionID);
                _chestExtinctionActive = false;
            }
            else
            {
                Plugin.Log.LogError("DivinitiesManager instance is null! Cannot deactivate Chest Extinction.");
            }
        }
        public string GetDivinityID(Divinity divinity)
        {

            if (divinity == null)
            {
                Plugin.Log.LogDebug("Divinity instance is null.");
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