using System.Globalization;

namespace ArmoryManager.Core;

internal sealed class ProtectedItemStore
{
    private readonly string _filePath;
    private readonly HashSet<uint> _uids = new();

    internal ProtectedItemStore(string filePath, string migratedUids)
    {
        _filePath = filePath;
        Load(File.Exists(_filePath) ? File.ReadAllText(_filePath) : string.Empty);
        Load(migratedUids);
        Save();
    }

    internal bool Contains(uint uid) => _uids.Contains(uid);

    internal bool Toggle(uint uid)
    {
        var isProtected = _uids.Add(uid);
        if (!isProtected)
            _uids.Remove(uid);

        Save();
        return isProtected;
    }

    internal void Prune(IEnumerable<uint> activeUids)
    {
        var active = activeUids.ToHashSet();
        if (_uids.RemoveWhere(uid => !active.Contains(uid)) > 0)
            Save();
    }

    private void Load(string serializedUids)
    {
        foreach (var part in (serializedUids ?? string.Empty)
                     .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (uint.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var uid))
                _uids.Add(uid);
        }
    }

    private void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(_filePath, string.Join(",", _uids.OrderBy(uid => uid)
                .Select(uid => uid.ToString(CultureInfo.InvariantCulture))));
        }
        catch (Exception exception)
        {
            Plugin.Logger.Error($"Could not save Armory favorites: {exception}");
        }
    }
}
