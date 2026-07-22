namespace ArmoryManager.Core;

internal static class GearTypes
{
    internal const string Sword = "Sword";
    internal const string Shield = "Shield";
    internal const string Armor = "Armor";
    internal const string Helmet = "Helmet";
    internal const string Boots = "Boots";
    internal const string Ring = "Ring";
    internal const string Dagger = "Dagger";
    internal const string Axe = "Axe";
    internal const string Staff = "Staff";
    internal const string Bow = "Bow";
    internal const string Spellbook = "Spellbook";
    internal const string Spirit = "Spirit";
    internal const string Pendant = "Pendant";
    internal const string Gloves = "Gloves";
    internal const string Cape = "Cape";
    internal const string Claw = "Claw";
    internal const string Spear = "Spear";
    internal const string Shuriken = "Shuriken";

    internal static readonly IReadOnlyList<string> All = new[]
    {
        Sword, Shield, Armor, Helmet, Boots, Ring, Dagger, Axe, Staff, Bow, Spellbook,
        Spirit, Pendant, Gloves, Cape, Claw, Spear, Shuriken
    };

    internal static string Normalize(string value)
    {
        var friendlyName = All.FirstOrDefault(type =>
            string.Equals(type, value, StringComparison.OrdinalIgnoreCase));
        if (friendlyName != null)
            return friendlyName;

        // Idle Slayer 7 exposes the equipment ScriptableObjects by these internal names.
        // Keep the public configuration readable while translating the runtime values here.
        return value?.Trim().ToLowerInvariant() switch
        {
            "equipment_1" => Sword,
            "equipment_2" => Shield,
            "equipment_3" => Armor,
            "equipment_4" => Helmet,
            "equipment_5" => Boots,
            "equipment_6" => Ring,
            "equipment_7" => Dagger,
            "equipment_8" => Axe,
            "equipment_9" => Staff,
            "equipment_10" => Bow,
            "equipment_11" => Spellbook,
            _ => null
        };
    }

    internal static string NormalizeUserInput(string value)
    {
        var compact = new string((value ?? string.Empty)
            .Where(character => !char.IsWhiteSpace(character) && character != '-' && character != '_')
            .ToArray());

        return compact.ToLowerInvariant() switch
        {
            "sword" or "swords" => Sword,
            "shield" or "shields" => Shield,
            "armor" or "armors" or "armour" or "armours" => Armor,
            "helmet" or "helmets" or "helm" or "helms" => Helmet,
            "boot" or "boots" => Boots,
            "ring" or "rings" => Ring,
            "dagger" or "daggers" => Dagger,
            "axe" or "axes" => Axe,
            "staff" or "staffs" or "staves" => Staff,
            "bow" or "bows" => Bow,
            "spellbook" or "spellbooks" or "book" or "books" => Spellbook,
            "spirit" or "spirits" => Spirit,
            "pendant" or "pendants" => Pendant,
            "glove" or "gloves" => Gloves,
            "cape" or "capes" => Cape,
            "claw" or "claws" => Claw,
            "spear" or "spears" => Spear,
            "shuriken" or "shurikens" => Shuriken,
            _ => null
        };
    }
}

[Flags]
internal enum BreakReason
{
    None = 0,
    LowLevel = 1,
    Duplicate = 2
}

internal sealed class ArmoryItemSnapshot
{
    internal ArmoryItemSnapshot(
        uint uid,
        string itemId,
        string gearType,
        int level,
        bool isExcellent,
        IReadOnlySet<string> options,
        IReadOnlySet<string> skills,
        bool isProtected,
        bool isEquipped,
        bool isKey = false,
        int storageIndex = -1)
    {
        Uid = uid;
        ItemId = itemId;
        RawGearType = gearType;
        GearType = GearTypes.Normalize(gearType);
        Level = level;
        IsExcellent = isExcellent;
        Options = options;
        Skills = skills;
        IsProtected = isProtected;
        IsEquipped = isEquipped;
        IsKey = isKey;
        StorageIndex = storageIndex;
    }

    internal uint Uid { get; }
    internal string ItemId { get; }
    internal string RawGearType { get; }
    internal string GearType { get; }
    internal int Level { get; }
    internal bool IsExcellent { get; }
    internal IReadOnlySet<string> Options { get; }
    internal IReadOnlySet<string> Skills { get; }
    internal bool IsProtected { get; }
    internal bool IsEquipped { get; }
    internal bool IsKey { get; }
    internal int StorageIndex { get; }
    internal bool IsRemovable => !IsKey && !IsProtected && !IsEquipped;
}

internal sealed class ArmoryRules
{
    internal ArmoryRules(
        bool breakLowLevel,
        int lowLevelMaximum,
        bool breakDuplicates,
        int desiredFreeSlots,
        IReadOnlySet<string> enabledGearTypes,
        int minimumItemsToKeep = 0)
    {
        BreakLowLevel = breakLowLevel;
        LowLevelMaximum = lowLevelMaximum;
        BreakDuplicates = breakDuplicates;
        DesiredFreeSlots = desiredFreeSlots;
        EnabledGearTypes = enabledGearTypes ?? throw new ArgumentNullException(nameof(enabledGearTypes));
        MinimumItemsToKeep = Math.Max(0, minimumItemsToKeep);
    }

    internal bool BreakLowLevel { get; }
    internal int LowLevelMaximum { get; }
    internal bool BreakDuplicates { get; }
    internal int DesiredFreeSlots { get; }
    internal IReadOnlySet<string> EnabledGearTypes { get; }
    internal int MinimumItemsToKeep { get; }
    internal bool Allows(ArmoryItemSnapshot item) => item.GearType != null && EnabledGearTypes.Contains(item.GearType);
}

internal sealed class BreakCandidate
{
    internal BreakCandidate(uint uid, string gearType, BreakReason reason)
    {
        Uid = uid;
        GearType = gearType;
        Reason = reason;
    }

    internal uint Uid { get; }
    internal string GearType { get; }
    internal BreakReason Reason { get; }
}

internal sealed class ArmoryStorageGridSnapshot
{
    internal ArmoryStorageGridSnapshot(
        int capacity,
        int occupiedSlots,
        bool isReliable,
        string failureReason = null)
    {
        Capacity = Math.Max(0, capacity);
        OccupiedSlots = Math.Clamp(occupiedSlots, 0, Capacity);
        IsReliable = isReliable
                     && capacity >= 0
                     && occupiedSlots >= 0
                     && occupiedSlots <= capacity;
        FailureReason = IsReliable
            ? null
            : failureReason ?? "the physical Armory grid could not be verified";
    }

    internal int Capacity { get; }
    internal int OccupiedSlots { get; }
    internal int FreeSlots => Math.Max(0, Capacity - OccupiedSlots);
    internal bool IsReliable { get; }
    internal string FailureReason { get; }
}

internal sealed class ArmoryEvaluation
{
    internal ArmoryEvaluation(
        IReadOnlyList<BreakCandidate> candidates,
        int currentFreeSlots,
        int projectedFreeSlots,
        int desiredFreeSlots,
        int currentItemCount,
        int minimumItemsToKeep,
        int candidatesBlockedByMinimum,
        int storageCapacity,
        bool isGridReliable,
        string gridFailureReason)
    {
        Candidates = candidates;
        CurrentFreeSlots = currentFreeSlots;
        ProjectedFreeSlots = projectedFreeSlots;
        DesiredFreeSlots = desiredFreeSlots;
        CurrentItemCount = currentItemCount;
        MinimumItemsToKeep = minimumItemsToKeep;
        CandidatesBlockedByMinimum = candidatesBlockedByMinimum;
        StorageCapacity = storageCapacity;
        IsGridReliable = isGridReliable;
        GridFailureReason = gridFailureReason;
    }

    internal IReadOnlyList<BreakCandidate> Candidates { get; }
    internal int CurrentFreeSlots { get; }
    internal int ProjectedFreeSlots { get; }
    internal int DesiredFreeSlots { get; }
    internal int CurrentItemCount { get; }
    internal int MinimumItemsToKeep { get; }
    internal int CandidatesBlockedByMinimum { get; }
    internal int StorageCapacity { get; }
    internal bool IsGridReliable { get; }
    internal string GridFailureReason { get; }
    internal bool HasCapacityShortfall => ProjectedFreeSlots < DesiredFreeSlots;
}
