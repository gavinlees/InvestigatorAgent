using InvestigatorAgent.Utils;
using Xunit;


namespace InvestigatorAgent.Tests.Utils;

public sealed class FeatureFolderMapperTests : IDisposable
{
    private readonly string _tempPath;

    public FeatureFolderMapperTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
        {
            Directory.Delete(_tempPath, recursive: true);
        }
    }

    [Fact]
    public void Constructor_EmptyDirectory_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new FeatureFolderMapper(" "));
    }

    [Fact]
    public void GetFeatureFolders_DirectoryDoesNotExist_ReturnsEmptyDictionary()
    {
        var mapper = new FeatureFolderMapper(Path.Combine(_tempPath, "nonexistent"));
        var result = mapper.GetFeatureFolders();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetFeatureFolders_ValidDirectory_ReturnsFolderMapping()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_tempPath, "feature1"));
        Directory.CreateDirectory(Path.Combine(_tempPath, "feature2"));

        var mapper = new FeatureFolderMapper(_tempPath);

        // Act
        var result = mapper.GetFeatureFolders();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("feature1"));
        Assert.True(result.ContainsKey("feature2"));
        Assert.Equal(Path.Combine(_tempPath, "feature1"), result["feature1"]);
    }
}
