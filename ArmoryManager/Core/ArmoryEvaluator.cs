namespace ArmoryManager.Core;

internal static class ArmoryEvaluator
{
    internal static ArmoryEvaluation Evaluate(
        IReadOnlyCollection<ArmoryItemSnapshot> items,
        ArmoryRules rules,
        int maxSlots) => Evaluate(
        items,
        rules,
        new ArmoryStorageGridSnapshot(
            maxSlots,
            items.Count(item => !item.IsKey && !item.IsEquipped),
            true));

    internal static ArmoryEvaluation Evaluate(
        IReadOnlyCollection<ArmoryItemSnapshot> items,
        ArmoryRules rules,
        ArmoryStorageGridSnapshot grid)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(grid);

        var normalItems = items.Where(item => !item.IsKey).ToArray();
        var storedItems = normalItems.Where(item => !item.IsEquipped).ToArray();
        var reasonsByUid = new Dictionary<uint, BreakReason>();
        var protectedGridUids = GetProtectedGridUids(storedItems, rules.MinimumItemsToKeep);

        if (rules.BreakLowLevel)
        {
            foreach (var item in normalItems.Where(item =>
                         item.IsRemovable && rules.Allows(item) && item.Level <= rules.LowLevelMaximum))
            {
                AddReason(reasonsByUid, item.Uid, BreakReason.LowLevel);
            }
        }

        if (rules.BreakDuplicates)
        {
            foreach (var group in normalItems.GroupBy(item => item.ItemId, StringComparer.Ordinal))
            {
                var variants = group.ToArray();
                if (variants.Length < 2)
                    continue;

                var keeper = ChooseDuplicateKeeper(variants, protectedGridUids);
                foreach (var target in variants.Where(item =>
                             item.Uid != keeper.Uid && item.IsRemovable && rules.Allows(item)))
                {
                    AddReason(reasonsByUid, target.Uid, BreakReason.Duplicate);
                }
            }
        }

        var candidateDetails = reasonsByUid
            .Select(pair =>
            {
                var item = normalItems.First(item => item.Uid == pair.Key);
                return new { Item = item, Candidate = new BreakCandidate(pair.Key, item.GearType, pair.Value) };
            })
            .ToArray();
        var candidates = candidateDetails
            .Where(result => !protectedGridUids.Contains(result.Item.Uid))
            .OrderByDescending(result => result.Candidate.Reason.HasFlag(BreakReason.Duplicate))
            .ThenBy(result => result.Item.StorageIndex)
            .ThenBy(result => result.Item.Level)
            .ThenBy(result => result.Item.Uid)
            .Select(result => result.Candidate)
            .ToArray();

        // Never act on an inferred inventory count. Production supplies a measurement taken from
        // every actual allSlots entry; an unreadable or internally inconsistent grid fails closed.
        if (!grid.IsReliable)
            candidates = Array.Empty<BreakCandidate>();

        var boundedMaxSlots = grid.Capacity;
        var storedItemCount = grid.OccupiedSlots;
        var currentFreeSlots = grid.FreeSlots;
        var projectedFreeSlots = Math.Min(boundedMaxSlots, currentFreeSlots + candidates.Length);
        var desiredFreeSlots = Math.Clamp(rules.DesiredFreeSlots, 0, Math.Max(0, boundedMaxSlots - 1));

        return new ArmoryEvaluation(
            candidates,
            currentFreeSlots,
            projectedFreeSlots,
            desiredFreeSlots,
            storedItemCount,
            rules.MinimumItemsToKeep,
            Math.Max(0, candidateDetails.Length - candidates.Length),
            boundedMaxSlots,
            grid.IsReliable,
            grid.FailureReason);
    }

    private static void AddReason(Dictionary<uint, BreakReason> reasons, uint uid, BreakReason reason)
    {
        reasons.TryGetValue(uid, out var current);
        reasons[uid] = current | reason;
    }

    private static ArmoryItemSnapshot ChooseDuplicateKeeper(
        IReadOnlyCollection<ArmoryItemSnapshot> variants,
        IReadOnlySet<uint> protectedGridUids)
    {
        return variants
            .OrderByDescending(item => !item.IsRemovable || protectedGridUids.Contains(item.Uid))
            .ThenByDescending(item => item.IsProtected)
            .ThenByDescending(item => item.IsEquipped)
            .ThenByDescending(item => item.Level)
            .ThenByDescending(item => item.IsExcellent)
            .ThenByDescending(item => item.Options.Count)
            .ThenByDescending(item => item.Skills.Count)
            .ThenBy(item => item.StorageIndex < 0 ? int.MaxValue : item.StorageIndex)
            .ThenBy(item => item.Uid)
            .First();
    }

    private static IReadOnlySet<uint> GetProtectedGridUids(
        IReadOnlyCollection<ArmoryItemSnapshot> storedItems,
        int count)
    {
        if (count <= 0)
            return new HashSet<uint>();

        // If the live grid cannot be read, fail closed for every cleanup rule.
        if (storedItems.Any(item => item.StorageIndex < 0))
            return storedItems.Select(item => item.Uid).ToHashSet();

        return storedItems
            .Where(item => item.StorageIndex < count)
            .Select(item => item.Uid)
            .ToHashSet();
    }
}
