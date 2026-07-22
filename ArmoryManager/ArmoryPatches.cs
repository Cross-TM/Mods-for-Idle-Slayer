using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ArmoryManager;

internal static class ArmoryPatches
{
    [HarmonyPatch(typeof(CloudSave), nameof(CloudSave.GetSave))]
    private static class RecoveryCloudDownloadPatch
    {
        private static bool Prefix(Il2CppSystem.Action successCallback)
        {
            if (!Plugin.IsCloudRecoveryBlocked)
                return true;

            Plugin.Logger.Warning(
                "RECOVERY CLOUD DOWNLOAD BLOCKED: continuing with the verified local pre-incident save.");
            successCallback?.Invoke();
            return false;
        }
    }

    [HarmonyPatch(typeof(ArmoryItemChest), nameof(ArmoryItemChest.OnTriggerEnter2D))]
    private static class ArmoryChestPatch
    {
        private static void Postfix() => ArmoryController.Instance?.RequestScan("armory chest opened");
    }

    [HarmonyPatch(typeof(ItemObject), nameof(ItemObject.OnBeginDrag))]
    private static class BeginDragPatch
    {
        private static void Postfix() => ArmoryController.Instance?.DragStarted();
    }

    [HarmonyPatch(typeof(ItemObject), nameof(ItemObject.OnEndDrag))]
    private static class EndDragPatch
    {
        private static void Postfix() => ArmoryController.Instance?.DragEnded();
    }

    [HarmonyPatch(typeof(ItemObject), nameof(ItemObject.OnPointerClick))]
    private static class ProtectItemPatch
    {
        private static bool Prefix(ItemObject __instance, PointerEventData eventData)
        {
            var controlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            var favoriteClick = controlPressed || eventData?.button == PointerEventData.InputButton.Right;
            return !favoriteClick || ArmoryController.Instance?.TryToggleProtection(__instance) != true;
        }
    }

    [HarmonyPatch(typeof(WeaponsManager), nameof(WeaponsManager.RefreshList), new[] { typeof(bool) })]
    private static class RefreshListPatch
    {
        private static void Postfix() => ArmoryController.Instance?.RefreshVisuals();
    }

    [HarmonyPatch(typeof(WeaponsManager), nameof(WeaponsManager.GenerateRandomWeapon),
        new[] { typeof(WeaponItem), typeof(bool) })]
    private static class GenerateSingleWeaponPatch
    {
        private static void Postfix() => ArmoryController.Instance?.RequestScan("weapon generated");
    }

    [HarmonyPatch(typeof(WeaponsManager), nameof(WeaponsManager.GenerateRandomWeapon),
        new[] { typeof(Il2CppReferenceArray<WeaponItem>), typeof(bool) })]
    private static class GenerateWeaponArrayPatch
    {
        private static void Postfix() => ArmoryController.Instance?.RequestScan("weapon generated");
    }

    [HarmonyPatch(typeof(DivinitiesManager), nameof(DivinitiesManager.ActivateDivinity))]
    private static class ActivateDivinityPatch
    {
        private static void Postfix() => DivinityChanged();
    }

    [HarmonyPatch(typeof(DivinitiesManager), nameof(DivinitiesManager.DeactivateDivinity))]
    private static class DeactivateDivinityPatch
    {
        private static void Postfix() => DivinityChanged();
    }

    [HarmonyPatch(typeof(DivinitiesManager), nameof(DivinitiesManager.ResetDivinities))]
    private static class ResetDivinitiesPatch
    {
        private static void Postfix() => DivinityChanged();
    }

    [HarmonyPatch(typeof(AscensionManager), nameof(AscensionManager.Ascend))]
    private static class AscendPatch
    {
        private static void Prefix() => ArmoryController.Instance?.SuspendForAscension();
    }

    private static void DivinityChanged()
    {
        ArmoryController.Instance?.RefreshVisuals();
        ArmoryController.Instance?.RequestScan("divinity state changed");
    }
}
