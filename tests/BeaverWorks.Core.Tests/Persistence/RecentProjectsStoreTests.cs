using BeaverWorks.Core.Models;
using BeaverWorks.Core.Persistence;

namespace BeaverWorks.Core.Tests.Persistence;

public class RecentProjectsStoreTests : IDisposable
{
    private readonly string _tempRoot;

    public RecentProjectsStoreTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"beaverworks-recent-projects-tests-{Guid.NewGuid()}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    [Fact]
    public void LoadRecent_NoRecordedProjects_ReturnsEmptyListRatherThanThrowing()
    {
        var store = new RecentProjectsStore(_tempRoot);

        var result = store.LoadRecent("anna");

        Assert.Empty(result);
    }

    [Fact]
    public void RecordOpened_InsertsNewEntryAtFront()
    {
        var store = new RecentProjectsStore(_tempRoot);
        var first = new RecentProjectEntry { Name = "Kitchen", FilePath = @"C:\projects\kitchen.bwproj" };
        var second = new RecentProjectEntry { Name = "Bathroom", FilePath = @"C:\projects\bathroom.bwproj" };

        store.RecordOpened("anna", first);
        store.RecordOpened("anna", second);
        var result = store.LoadRecent("anna");

        Assert.Equal(2, result.Count);
        Assert.Equal("Bathroom", result[0].Name);
        Assert.Equal("Kitchen", result[1].Name);
    }

    [Fact]
    public void RecordOpened_ExistingFilePath_MovesToFrontInsteadOfDuplicating()
    {
        var store = new RecentProjectsStore(_tempRoot);
        var kitchen = new RecentProjectEntry { Name = "Kitchen", FilePath = @"C:\projects\kitchen.bwproj" };
        var bathroom = new RecentProjectEntry { Name = "Bathroom", FilePath = @"C:\projects\bathroom.bwproj" };

        store.RecordOpened("anna", kitchen);
        store.RecordOpened("anna", bathroom);
        store.RecordOpened("anna", kitchen);
        var result = store.LoadRecent("anna");

        Assert.Equal(2, result.Count);
        Assert.Equal("Kitchen", result[0].Name);
        Assert.Equal("Bathroom", result[1].Name);
    }
}
