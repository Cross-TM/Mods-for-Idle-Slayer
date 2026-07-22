using ArmoryManager.Core;
using Xunit;

namespace ArmoryManager.Tests;

public sealed class ProtectedItemStoreTests
{
    [Fact]
    public void MigratesOldConfigUidsIntoDedicatedFavoritesFile()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"ArmoryManagerFavorites-{Guid.NewGuid():N}.dat");
        try
        {
            File.WriteAllText(filePath, "10,20");

            var store = new ProtectedItemStore(filePath, "20,30");

            Assert.True(store.Contains(10));
            Assert.True(store.Contains(20));
            Assert.True(store.Contains(30));
            Assert.Equal("10,20,30", File.ReadAllText(filePath));
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Fact]
    public void TogglePersistsFavoriteImmediately()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"ArmoryManagerFavorites-{Guid.NewGuid():N}.dat");
        try
        {
            var store = new ProtectedItemStore(filePath, string.Empty);

            Assert.True(store.Toggle(42));
            Assert.Equal("42", File.ReadAllText(filePath));
            Assert.False(store.Toggle(42));
            Assert.Equal(string.Empty, File.ReadAllText(filePath));
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
