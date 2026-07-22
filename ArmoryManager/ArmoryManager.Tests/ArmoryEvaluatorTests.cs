using ArmoryManager.Core;
using Xunit;

namespace ArmoryManager.Tests;

public sealed class ArmoryEvaluatorTests
{
    [Fact]
    public void LowLevelRuleIncludesExcellentButExcludesProtectedEquippedAndKeyItems()
    {
        var items = new[]
        {
            Item(1, "Sword", 3, excellent: true),
            Item(2, "Bow", 3, isProtected: true),
            Item(3, "Shield", 3, isEquipped: true),
            Item(4, "Key", 1, isKey: true),
            Item(5, "Boots", 4)
        };

        var result = Evaluate(items, lowLevel: true, lowLevelMaximum: 3, duplicates: false);

        Assert.Collection(result.Candidates,
            candidate =>
            {
                Assert.Equal((uint)1, candidate.Uid);
                Assert.Equal(BreakReason.LowLevel, candidate.Reason);
            });
    }

    [Fact]
    public void ExactDuplicatesKeepTheLowestUid()
    {
        var items = new[]
        {
            Item(30, "Sword", 8, options: new[] { "A" }, skills: new[] { "S" }),
            Item(10, "Sword", 8, options: new[] { "A" }, skills: new[] { "S" }),
            Item(20, "Sword", 8, options: new[] { "A" }, skills: new[] { "S" })
        };

        var result = Evaluate(items, lowLevel: false, duplicates: true);

        Assert.Equal(new uint[] { 20, 30 }, result.Candidates.Select(candidate => candidate.Uid));
    }

    [Fact]
    public void DuplicateRulePrioritizesLevelBeforeExcellentAndOptions()
    {
        var items = new[]
        {
            Item(1, "Sword", 10, excellent: true, options: new[] { "A", "B" }, skills: new[] { "S" }),
            Item(2, "Sword", 9, options: new[] { "A" }, skills: new[] { "S" }),
            Item(3, "Sword", 11, options: new[] { "A" }, skills: new[] { "S" }),
            Item(4, "Sword", 10, excellent: true, options: new[] { "A" }, skills: new[] { "Other" })
        };

        var result = Evaluate(items, lowLevel: false, duplicates: true);

        Assert.Equal(new uint[] { 1, 2, 4 },
            result.Candidates.Select(candidate => candidate.Uid).OrderBy(uid => uid));
        Assert.All(result.Candidates,
            candidate => Assert.Equal(BreakReason.Duplicate, candidate.Reason));
    }

    [Fact]
    public void ProtectedVariantCanBeTheKeeperButNeverTheCandidate()
    {
        var items = new[]
        {
            Item(1, "Bow", 7, excellent: true, options: new[] { "A" }, isProtected: true),
            Item(2, "Bow", 6, options: new[] { "A" })
        };

        var result = Evaluate(items, lowLevel: false, duplicates: true);

        Assert.Single(result.Candidates);
        Assert.Equal((uint)2, result.Candidates[0].Uid);
    }

    [Fact]
    public void InternalNameRatherThanDisplayNameControlsDuplicateGroups()
    {
        var items = new[]
        {
            Item(1, "SwordInternal", 4),
            Item(2, "OtherInternal", 4)
        };

        var result = Evaluate(items, lowLevel: false, duplicates: true);

        Assert.Empty(result.Candidates);
    }

    [Fact]
    public void CandidateCanContainBothReasons()
    {
        var items = new[]
        {
            Item(1, "Sword", 3),
            Item(2, "Sword", 2)
        };

        var result = Evaluate(items, lowLevel: true, lowLevelMaximum: 3, duplicates: true);
        var second = Assert.Single(result.Candidates, candidate => candidate.Uid == 2);

        Assert.True(second.Reason.HasFlag(BreakReason.LowLevel));
        Assert.True(second.Reason.HasFlag(BreakReason.Duplicate));
    }

    [Fact]
    public void CapacityShortfallNeverCreatesUniqueCandidates()
    {
        var items = new[]
        {
            Item(1, "Sword", 10),
            Item(2, "Bow", 10),
            Item(3, "Shield", 10),
            Item(4, "Boots", 10)
        };

        var result = ArmoryEvaluator.Evaluate(items,
            Rules(false, 3, true, 2), maxSlots: 5);

        Assert.Empty(result.Candidates);
        Assert.Equal(1, result.CurrentFreeSlots);
        Assert.Equal(1, result.ProjectedFreeSlots);
        Assert.True(result.HasCapacityShortfall);
    }

    [Fact]
    public void EquippedItemsDoNotConsumeStorageSlots()
    {
        var items = new[]
        {
            Item(1, "EquippedSword", 10, isEquipped: true),
            Item(2, "EquippedShield", 10, isEquipped: true),
            Item(3, "StoredSword", 10),
            Item(4, "StoredShield", 10)
        };

        var result = ArmoryEvaluator.Evaluate(items,
            Rules(false, 4, false, 1), maxSlots: 3);

        Assert.Equal(1, result.CurrentFreeSlots);
        Assert.Equal(1, result.ProjectedFreeSlots);
        Assert.Equal(2, result.CurrentItemCount);
        Assert.False(result.HasCapacityShortfall);
    }

    [Fact]
    public void PhysicalGridMeasurementIsTheOnlySourceForFreeSlots()
    {
        var items = Enumerable.Range(0, 20)
            .Select(index => Item((uint)(index + 1), $"Stored{index}", 10, storageIndex: index))
            .Concat(Enumerable.Range(0, 11)
                .Select(index => Item((uint)(100 + index), $"Equipped{index}", 10, isEquipped: true)))
            .ToArray();
        var grid = new ArmoryStorageGridSnapshot(capacity: 58, occupiedSlots: 20, isReliable: true);

        var result = ArmoryEvaluator.Evaluate(items, Rules(false, 4, false, 2), grid);

        Assert.True(result.IsGridReliable);
        Assert.Equal(58, result.StorageCapacity);
        Assert.Equal(20, result.CurrentItemCount);
        Assert.Equal(38, result.CurrentFreeSlots);
        Assert.Equal(38, result.ProjectedFreeSlots);
        Assert.Equal(2, result.DesiredFreeSlots);
        Assert.False(result.HasCapacityShortfall);
    }

    [Fact]
    public void UnverifiedPhysicalGridAlwaysBlocksEveryDeletion()
    {
        var items = new[]
        {
            Item(1, "Duplicate", 1, storageIndex: 0),
            Item(2, "Duplicate", 2, storageIndex: 1)
        };
        var grid = new ArmoryStorageGridSnapshot(
            capacity: 58,
            occupiedSlots: 2,
            isReliable: false,
            failureReason: "test mismatch");

        var result = ArmoryEvaluator.Evaluate(items, Rules(true, 4, true, 2), grid);

        Assert.False(result.IsGridReliable);
        Assert.Equal("test mismatch", result.GridFailureReason);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public void ProtectedGridSlotsCountOnlyStoredItems()
    {
        var items = new[]
        {
            Item(1, "EquippedSword", 1, isEquipped: true),
            Item(2, "StoredA", 1, storageIndex: 0),
            Item(3, "StoredB", 2, storageIndex: 1),
            Item(4, "StoredC", 3, storageIndex: 2)
        };
        var rules = new ArmoryRules(true, 4, false, 1,
            GearTypes.All.ToHashSet(StringComparer.Ordinal), minimumItemsToKeep: 2);

        var result = ArmoryEvaluator.Evaluate(items, rules, maxSlots: 3);

        Assert.Single(result.Candidates);
        Assert.Equal(3, result.CurrentItemCount);
        Assert.Equal(2, result.MinimumItemsToKeep);
        Assert.Equal(2, result.CandidatesBlockedByMinimum);
    }

    [Fact]
    public void DesiredFreeSlotsAreClampedBelowMaximumCapacity()
    {
        var result = ArmoryEvaluator.Evaluate(Array.Empty<ArmoryItemSnapshot>(),
            Rules(false, 3, false, 99), maxSlots: 5);

        Assert.Equal(4, result.DesiredFreeSlots);
    }

    [Fact]
    public void GearAllowlistAppliesToEveryDestructiveRule()
    {
        var items = new[]
        {
            Item(1, "ShieldA", 4, gearType: GearTypes.Shield),
            Item(2, "ArmorA", 4, gearType: GearTypes.Armor),
            Item(3, "ArmorDupe", 8, gearType: GearTypes.Armor),
            Item(4, "ArmorDupe", 7, gearType: GearTypes.Armor)
        };
        var rules = new ArmoryRules(true, 4, true, 1,
            new HashSet<string>(StringComparer.Ordinal) { GearTypes.Shield });

        var result = ArmoryEvaluator.Evaluate(items, rules, maxSlots: 20);

        var candidate = Assert.Single(result.Candidates);
        Assert.Equal((uint)1, candidate.Uid);
        Assert.Equal(GearTypes.Shield, candidate.GearType);
    }

    [Fact]
    public void UnknownGearTypesFailClosed()
    {
        var items = new[] { Item(1, "FutureItem", 1, gearType: "FutureSlot") };

        var result = ArmoryEvaluator.Evaluate(items, Rules(true, 4, true, 1), maxSlots: 20);

        Assert.Empty(result.Candidates);
    }

    [Fact]
    public void MinimumItemsToKeepProtectsTheFirstGridPositions()
    {
        var items = new[]
        {
            Item(1, "SwordA", 4, storageIndex: 0),
            Item(2, "SwordB", 2, storageIndex: 1),
            Item(3, "SwordC", 3, storageIndex: 2),
            Item(4, "SwordD", 1, storageIndex: 3)
        };
        var rules = new ArmoryRules(true, 4, true, 1,
            GearTypes.All.ToHashSet(StringComparer.Ordinal), minimumItemsToKeep: 3);

        var result = ArmoryEvaluator.Evaluate(items, rules, maxSlots: 20);

        var candidate = Assert.Single(result.Candidates);
        Assert.Equal((uint)4, candidate.Uid);
        Assert.Equal(4, result.CurrentItemCount);
        Assert.Equal(3, result.MinimumItemsToKeep);
        Assert.Equal(3, result.CandidatesBlockedByMinimum);
    }

    [Fact]
    public void TwentyProtectedSlotsMeansFirstFourRowsOfFive()
    {
        var items = Enumerable.Range(0, 25)
            .Select(index => Item((uint)(index + 1), $"Unique{index}", 1, storageIndex: index))
            .ToArray();
        var rules = new ArmoryRules(true, 4, false, 1,
            GearTypes.All.ToHashSet(StringComparer.Ordinal), minimumItemsToKeep: 20);

        var result = ArmoryEvaluator.Evaluate(items, rules, maxSlots: 25);

        Assert.Equal(new uint[] { 21, 22, 23, 24, 25 },
            result.Candidates.Select(candidate => candidate.Uid));
        Assert.Equal(20, result.CandidatesBlockedByMinimum);
    }

    [Fact]
    public void TwentyProtectedSlotsMeansPositionsOneThroughTwentyEvenWhenThereAreHoles()
    {
        var items = new[]
        {
            Item(1, "AtFirstPosition", 1, storageIndex: 0),
            Item(2, "AtTwentiethPosition", 1, storageIndex: 19),
            Item(3, "AtTwentyFirstPosition", 1, storageIndex: 20),
            Item(4, "AtLastPosition", 1, storageIndex: 57)
        };
        var rules = new ArmoryRules(true, 4, false, 2,
            GearTypes.All.ToHashSet(StringComparer.Ordinal), minimumItemsToKeep: 20);
        var grid = new ArmoryStorageGridSnapshot(capacity: 58, occupiedSlots: 4, isReliable: true);

        var result = ArmoryEvaluator.Evaluate(items, rules, grid);

        Assert.Equal(new uint[] { 3, 4 }, result.Candidates.Select(candidate => candidate.Uid));
        Assert.Equal(2, result.CandidatesBlockedByMinimum);
        Assert.Equal(54, result.CurrentFreeSlots);
    }

    [Fact]
    public void PhysicalFreeSlotTargetUsesMeasuredOccupancyAndProjectedCleanup()
    {
        var items = new[]
        {
            Item(1, "UniqueA", 1, storageIndex: 0),
            Item(2, "UniqueB", 1, storageIndex: 1),
            Item(3, "UniqueC", 10, storageIndex: 2)
        };
        var grid = new ArmoryStorageGridSnapshot(capacity: 3, occupiedSlots: 3, isReliable: true);

        var result = ArmoryEvaluator.Evaluate(items, Rules(true, 4, false, 2), grid);

        Assert.Equal(0, result.CurrentFreeSlots);
        Assert.Equal(2, result.ProjectedFreeSlots);
        Assert.Equal(2, result.DesiredFreeSlots);
        Assert.False(result.HasCapacityShortfall);
        Assert.Equal(new uint[] { 1, 2 }, result.Candidates.Select(candidate => candidate.Uid));
    }

    [Fact]
    public void ProtectedPhysicalSlotsOverrideDuplicateCleanup()
    {
        var items = new List<ArmoryItemSnapshot>
        {
            Item(1, "Duplicate", 12, storageIndex: 0)
        };
        items.AddRange(Enumerable.Range(1, 19)
            .Select(index => Item((uint)(index + 1), $"Unique{index}", 10, storageIndex: index)));
        items.Add(Item(21, "Duplicate", 9, excellent: true, storageIndex: 20));
        var rules = new ArmoryRules(true, 4, true, 1,
            GearTypes.All.ToHashSet(StringComparer.Ordinal), minimumItemsToKeep: 20);

        var result = ArmoryEvaluator.Evaluate(items, rules, maxSlots: 21);

        var candidate = Assert.Single(result.Candidates);
        Assert.Equal((uint)21, candidate.Uid);
        Assert.Equal(BreakReason.Duplicate, candidate.Reason);
        Assert.Equal(0, result.CandidatesBlockedByMinimum);
    }

    [Fact]
    public void HigherLevelDuplicateWinsOverLowerExcellentDuplicate()
    {
        var items = new[]
        {
            Item(3453, "Kishar", 12),
            Item(3800, "Kishar", 9, excellent: true)
        };

        var result = Evaluate(items, lowLevel: false, duplicates: true);

        var candidate = Assert.Single(result.Candidates);
        Assert.Equal((uint)3800, candidate.Uid);
        Assert.Equal(BreakReason.Duplicate, candidate.Reason);
    }

    [Fact]
    public void UnknownGridPositionFailsClosedForEveryCleanupRule()
    {
        var items = new[]
        {
            Item(1, "Duplicate", 1),
            Item(2, "Duplicate", 2)
        };
        var rules = new ArmoryRules(true, 4, true, 1,
            GearTypes.All.ToHashSet(StringComparer.Ordinal), minimumItemsToKeep: 20);

        var result = ArmoryEvaluator.Evaluate(items, rules, maxSlots: 25);

        Assert.Empty(result.Candidates);
        Assert.Equal(2, result.CandidatesBlockedByMinimum);
    }

    [Theory]
    [InlineData("shields", GearTypes.Shield)]
    [InlineData("Armour", GearTypes.Armor)]
    [InlineData("AXES", GearTypes.Axe)]
    [InlineData("  Cape  ", GearTypes.Cape)]
    [InlineData("Staff", GearTypes.Staff)]
    [InlineData("Helmets", GearTypes.Helmet)]
    [InlineData("Boots", GearTypes.Boots)]
    [InlineData("Spell Books", GearTypes.Spellbook)]
    public void GearTypeInputAcceptsFriendlyNames(string input, string expected)
    {
        Assert.Equal(expected, GearTypes.NormalizeUserInput(input));
    }

    [Theory]
    [InlineData("equipment_1", GearTypes.Sword)]
    [InlineData("equipment_2", GearTypes.Shield)]
    [InlineData("equipment_3", GearTypes.Armor)]
    [InlineData("equipment_4", GearTypes.Helmet)]
    [InlineData("equipment_5", GearTypes.Boots)]
    [InlineData("equipment_6", GearTypes.Ring)]
    [InlineData("equipment_7", GearTypes.Dagger)]
    [InlineData("equipment_8", GearTypes.Axe)]
    [InlineData("equipment_9", GearTypes.Staff)]
    [InlineData("equipment_10", GearTypes.Bow)]
    [InlineData("equipment_11", GearTypes.Spellbook)]
    public void RuntimeEquipmentNamesMapToFriendlyGearTypes(string input, string expected)
    {
        Assert.Equal(expected, GearTypes.Normalize(input));
    }

    private static ArmoryEvaluation Evaluate(
        IReadOnlyCollection<ArmoryItemSnapshot> items,
        bool lowLevel,
        int lowLevelMaximum = 3,
        bool duplicates = false) =>
        ArmoryEvaluator.Evaluate(items,
            Rules(lowLevel, lowLevelMaximum, duplicates, 1), maxSlots: 20);

    private static ArmoryRules Rules(
        bool lowLevel,
        int lowLevelMaximum,
        bool duplicates,
        int desiredFreeSlots) => new(
        lowLevel,
        lowLevelMaximum,
        duplicates,
        desiredFreeSlots,
        GearTypes.All.ToHashSet(StringComparer.Ordinal));

    private static ArmoryItemSnapshot Item(
        uint uid,
        string id,
        int level,
        bool excellent = false,
        IEnumerable<string>? options = null,
        IEnumerable<string>? skills = null,
        bool isProtected = false,
        bool isEquipped = false,
        bool isKey = false,
        string gearType = GearTypes.Sword,
        int storageIndex = -1) => new(
        uid,
        id,
        gearType,
        level,
        excellent,
        (options ?? Array.Empty<string>()).ToHashSet(StringComparer.Ordinal),
        (skills ?? Array.Empty<string>()).ToHashSet(StringComparer.Ordinal),
        isProtected,
        isEquipped,
        isKey,
        storageIndex);
}
