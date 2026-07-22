using System.Globalization;
using ArmoryManager.Core;
using Il2Cpp;

namespace ArmoryManager.Game;

internal sealed class ArmoryInventorySnapshot
{
    internal ArmoryInventorySnapshot(
        IReadOnlyList<ArmoryItemSnapshot> items,
        ArmoryStorageGridSnapshot grid)
    {
        Items = items;
        Grid = grid;
    }

    internal IReadOnlyList<ArmoryItemSnapshot> Items { get; }
    internal ArmoryStorageGridSnapshot Grid { get; }
}

internal sealed class ArmorySnapshotFactory
{
    private readonly WeaponsManager _weaponsManager;
    private readonly ProtectedItemStore _protectedItems;

    internal ArmorySnapshotFactory(WeaponsManager weaponsManager, ProtectedItemStore protectedItems)
    {
        _weaponsManager = weaponsManager;
        _protectedItems = protectedItems;
    }

    internal ArmoryInventorySnapshot Create()
    {
        var items = new List<WeaponItemInventory>();
        foreach (var item in _weaponsManager.currentItemsByUid.Values)
            items.Add(item);

        _protectedItems.Prune(items.Select(item => item.uid));

        var gridRead = ReadStorageGrid();

        var snapshots = items.Select(item => CreateSnapshot(
            item,
            gridRead.StorageIndexes.TryGetValue(item.uid, out var index) ? index : -1)).ToArray();

        // Empty WeaponItemSlots do not contain an ItemObject child. Reliability therefore comes
        // from the inverse check: every normal, non-equipped inventory item must map back to one
        // and only one physical slot.
        var unmappedStored = snapshots.Count(item => !item.IsEquipped && item.StorageIndex < 0);
        var mappedStored = snapshots.Count(item => !item.IsEquipped && item.StorageIndex >= 0);
        var gridMismatch = unmappedStored > 0 || mappedStored != gridRead.Grid.OccupiedSlots;
        var grid = !gridMismatch
            ? gridRead.Grid
            : new ArmoryStorageGridSnapshot(
                gridRead.Grid.Capacity,
                gridRead.Grid.OccupiedSlots,
                false,
                $"physical grid/inventory mismatch: occupied={gridRead.Grid.OccupiedSlots}, " +
                $"mapped={mappedStored}, unmapped={unmappedStored}");

        return new ArmoryInventorySnapshot(snapshots, grid);
    }

    internal ArmoryItemSnapshot CreateSnapshot(WeaponItemInventory item, int storageIndex = -1) => new(
        item.uid,
        item.item?.name ?? $"unknown:{item.uid}",
        item.item?.forEquipment?.name,
        item.currentLevel,
        item.isExcellent,
        ToOptionSet(item),
        ToSkillSet(item),
        _protectedItems.Contains(item.uid),
        IsEquipped(item),
        storageIndex: storageIndex);

    private GridReadResult ReadStorageGrid()
    {
        var result = new Dictionary<uint, int>();
        var slots = _weaponsManager.allSlots;
        if (slots == null || slots.Length == 0)
            return new GridReadResult(
                result,
                new ArmoryStorageGridSnapshot(0, 0, false, "the physical slot array is not available"));

        var occupied = 0;
        string failureReason = null;

        for (var index = 0; index < slots.Length; index++)
        {
            var slot = slots[index];
            if (slot == null)
            {
                failureReason ??= $"physical slot {index + 1} is unavailable";
                continue;
            }

            var itemObject = slot.GetComponentInChildren<ItemObject>();
            if (itemObject == null)
            {
                // The game only creates this child for an occupied slot.
                continue;
            }

            var item = itemObject.currentItem;
            if (item == null)
                continue;

            occupied++;
            if (!_weaponsManager.currentItemsByUid.ContainsKey(item.uid))
            {
                failureReason ??= $"physical slot {index + 1} contains an item absent from the live inventory";
                continue;
            }

            if (!result.TryAdd(item.uid, index))
                failureReason ??= $"UID {item.uid} appears in more than one physical slot";
        }

        return new GridReadResult(
            result,
            new ArmoryStorageGridSnapshot(slots.Length, occupied, failureReason == null, failureReason));
    }

    private sealed class GridReadResult
    {
        internal GridReadResult(
            Dictionary<uint, int> storageIndexes,
            ArmoryStorageGridSnapshot grid)
        {
            StorageIndexes = storageIndexes;
            Grid = grid;
        }

        internal Dictionary<uint, int> StorageIndexes { get; }
        internal ArmoryStorageGridSnapshot Grid { get; }
    }

    private bool IsEquipped(WeaponItemInventory item)
    {
        if (item.IsEquipped())
            return true;

        return _weaponsManager.activeLoadoutUids?.Contains(item.uid) == true
               || _weaponsManager.tempEquippedUids?.Contains(item.uid) == true;
    }

    private static IReadOnlySet<string> ToOptionSet(WeaponItemInventory item)
    {
        if (item.currentOptions == null)
            return new HashSet<string>(StringComparer.Ordinal);

        var options = new HashSet<string>(StringComparer.Ordinal);
        foreach (var option in item.currentOptions)
        {
            if (option != null)
                options.Add(option.Pointer.ToInt64().ToString("X16", CultureInfo.InvariantCulture));
        }

        return options;
    }

    private static IReadOnlySet<string> ToSkillSet(WeaponItemInventory item)
    {
        if (item.currentSkills == null)
            return new HashSet<string>(StringComparer.Ordinal);

        var skills = new HashSet<string>(StringComparer.Ordinal);
        foreach (var skill in item.currentSkills)
        {
            if (skill != null)
                skills.Add(skill.name ?? skill.Pointer.ToInt64().ToString("X16", CultureInfo.InvariantCulture));
        }

        return skills;
    }
}
