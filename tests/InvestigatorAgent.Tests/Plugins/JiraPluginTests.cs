using InvestigatorAgent.Plugins;
using InvestigatorAgent.Utils;
using Xunit;
using System.Text.Json;

namespace InvestigatorAgent.Tests.Plugins;

public sealed class JiraPluginTests : IDisposable
{
    private readonly string _tempPath;

    public JiraPluginTests()
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
    public void Constructor_NullMapper_ThrowsArgumentNullException()
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        Assert.Throws<ArgumentNullException>(() => new JiraPlugin(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

    [Fact]
    public void GetJiraData_ValidData_ReturnsJsonSummary()
    {
        // Arrange
        var featureDir = Path.Combine(_tempPath, "feature1");
        var jiraDir = Path.Combine(featureDir, "jira");
        Directory.CreateDirectory(jiraDir);

        string jsonContent = """
        {
          "key": "TEST-123",
          "fields": {
            "summary": "Implement Feature",
            "status": {
              "name": "In Progress"
            }
          }
        }
        """;
        File.WriteAllText(Path.Combine(jiraDir, "feature_issue.json"), jsonContent);

        var mapper = new FeatureFolderMapper(_tempPath);
        var plugin = new JiraPlugin(mapper);

        // Act
        string resultJson = plugin.GetJiraData();

        // Assert
        using var document = JsonDocument.Parse(resultJson);
        var root = document.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(1, root.GetArrayLength());

        var firstFeature = root[0];
        Assert.Equal("feature1", firstFeature.GetProperty("id").GetString());
        Assert.Equal("TEST-123", firstFeature.GetProperty("key").GetString());
        Assert.Equal("Implement Feature", firstFeature.GetProperty("summary").GetString());
        Assert.Equal("In Progress", firstFeature.GetProperty("status").GetString());
    }

    [Fact]
    public void GetJiraData_MissingFile_ReturnsMissingStatus()
    {
        // Arrange
        var featureDir = Path.Combine(_tempPath, "feature2");
        Directory.CreateDirectory(featureDir); // Setup folder but no Jira subfolder/file

        var mapper = new FeatureFolderMapper(_tempPath);
        var plugin = new JiraPlugin(mapper);

        // Act
        string resultJson = plugin.GetJiraData();

        // Assert
        using var document = JsonDocument.Parse(resultJson);
        var root = document.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(1, root.GetArrayLength());

        var feature = root[0];
        Assert.Equal("feature2", feature.GetProperty("id").GetString());
        Assert.Equal("Missing", feature.GetProperty("key").GetString());
        Assert.Equal("Unknown", feature.GetProperty("status").GetString());
    }

    [Fact]
    public void GetJiraData_InvalidJson_ReturnsErrorStatus()
    {
        // Arrange
        var featureDir = Path.Combine(_tempPath, "feature3");
        var jiraDir = Path.Combine(featureDir, "jira");
        Directory.CreateDirectory(jiraDir);

        // Write invalid JSON
        File.WriteAllText(Path.Combine(jiraDir, "feature_issue.json"), "{ invalid: json ");

        var mapper = new FeatureFolderMapper(_tempPath);
        var plugin = new JiraPlugin(mapper);

        // Act
        string resultJson = plugin.GetJiraData();

        // Assert
        using var document = JsonDocument.Parse(resultJson);
        var root = document.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(1, root.GetArrayLength());

        var feature = root[0];
        Assert.Equal("feature3", feature.GetProperty("id").GetString());
        Assert.Equal("Error", feature.GetProperty("key").GetString());
        Assert.StartsWith("Failed to read data", feature.GetProperty("summary").GetString());
        Assert.Equal("Error", feature.GetProperty("status").GetString());
    }
}
