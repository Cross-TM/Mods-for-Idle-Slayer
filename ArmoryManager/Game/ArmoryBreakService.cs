using System.Globalization;
using ArmoryManager.Core;
using Il2Cpp;

namespace ArmoryManager.Game;

internal sealed class BreakBatchResult
{
    internal BreakBatchResult(int brokenCount, int divinityPoints, int scrap)
    {
        BrokenCount = brokenCount;
        DivinityPoints = divinityPoints;
        Scrap = scrap;
    }

    internal int BrokenCount { get; }
    internal int DivinityPoints { get; }
    internal int Scrap { get; }
}

internal sealed class ArmoryBreakService
{
    private readonly WeaponsManager _weaponsManager;
    private readonly PlayerInventory _playerInventory;
    private readonly DropsManager _dropsManager;

    internal ArmoryBreakService(
        WeaponsManager weaponsManager,
        PlayerInventory playerInventory,
        DropsManager dropsManager)
    {
        _weaponsManager = weaponsManager;
        _playerInventory = playerInventory;
        _dropsManager = dropsManager;
    }

    internal BreakBatchResult Execute(
        IReadOnlyList<BreakCandidate> candidates,
        Func<uint, bool> isStillCandidate)
    {
        var brokenCount = 0;
        var divinityPoints = 0;
        var scrap = 0;

        foreach (var candidate in candidates)
        {
            try
            {
                if (!isStillCandidate(candidate.Uid)
                    || !_weaponsManager.currentItemsByUid.TryGetValue(candidate.Uid, out var item)
                    || item == null
                    || item.IsEquipped()
                    || _weaponsManager.activeLoadoutUids?.Contains(candidate.Uid) == true
                    || _weaponsManager.tempEquippedUids?.Contains(candidate.Uid) == true)
                {
                    continue;
                }

                var itemDivinityPoints = item.CalculateDivinityPointsRefund();
                var itemScrap = item.GetScrapValue();
                if (!_weaponsManager.currentItemsByUid.Remove(candidate.Uid))
                    continue;

                brokenCount++;
                divinityPoints += itemDivinityPoints;
                scrap += itemScrap;
                Plugin.Logger.Msg(
                    $"Broke UID {candidate.Uid} ({candidate.GearType}, {item.item?.name ?? "unknown"}, " +
                    $"level {item.currentLevel}) " +
                    $"because {candidate.Reason}; +{itemDivinityPoints} DP, +{itemScrap} scrap.");
            }
            catch (Exception exception)
            {
                Plugin.Logger.Error($"Could not break UID {candidate.Uid}: {exception}");
            }
        }

        if (brokenCount == 0)
            return new BreakBatchResult(0, 0, 0);

        _playerInventory.divinityPoints += divinityPoints;
        _playerInventory.weaponsBroken += brokenCount;

        if (scrap > 0 && _dropsManager != null)
            _dropsManager.AddDrop(scrap, Drops.list.Scrap);

        SaveManager.SetString("Divinity Points",
            _playerInventory.divinityPoints.ToString(CultureInfo.InvariantCulture));
        SaveManager.SetString("Weapons Broken",
            _playerInventory.weaponsBroken.ToString(CultureInfo.InvariantCulture));

        _weaponsManager.SaveWeapons();
        _playerInventory.CalculateValues();
        _weaponsManager.RefreshList(false);
        AudioManager.instance?.Play("Item Broke");

        return new BreakBatchResult(brokenCount, divinityPoints, scrap);
    }
}
