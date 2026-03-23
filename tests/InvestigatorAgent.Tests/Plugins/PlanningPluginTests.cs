using InvestigatorAgent.Plugins;
using InvestigatorAgent.Utils;
using NSubstitute;
using Xunit;
using Polly;
using Polly.Retry;
using Microsoft.SemanticKernel;
using System.Reflection;

namespace InvestigatorAgent.Tests.Plugins;

public class PlanningPluginTests : IDisposable
{
    private readonly string _tempPath;
    private readonly IFeatureFolderMapper _mockMapper;
    private readonly AsyncRetryPolicy _retryPolicy;

    public PlanningPluginTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempPath);
        
        _mockMapper = Substitute.For<IFeatureFolderMapper>();
        _retryPolicy = Policy.Handle<Exception>().RetryAsync(0);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
        {
            Directory.Delete(_tempPath, true);
        }
    }

    private void CreateFeatureDir(string featureId, string folderName)
    {
        string path = Path.Combine(_tempPath, folderName);
        Directory.CreateDirectory(path);
        Directory.CreateDirectory(Path.Combine(path, "planning"));
        
        var folders = new Dictionary<string, string> { { featureId, path } };
        _mockMapper.GetFeatureFolders().Returns(folders);
    }

    private void CreatePlanningFile(string folderName, string fileName, string content)
    {
        string path = Path.Combine(_tempPath, folderName, "planning", fileName);
        File.WriteAllText(path, content);
    }

    [Fact]
    public async Task ListPlanningDocs_ReturnsFileList_WhenDocsExist()
    {
        // Arrange
        CreateFeatureDir("feat1", "feature_feat1");
        CreatePlanningFile("feature_feat1", "USER_STORY.md", "content");
        CreatePlanningFile("feature_feat1", "DESIGN.md", "content");
        var plugin = new PlanningPlugin(_tempPath, _mockMapper, _retryPolicy);

        // Act
        var result = await plugin.ListPlanningDocsAsync("feat1");

        // Assert
        Assert.Contains("USER_STORY.md", result);
        Assert.Contains("DESIGN.md", result);
    }

    [Fact]
    public async Task ListPlanningDocs_ReturnsError_WhenFeatureNotFound()
    {
        // Arrange
        _mockMapper.GetFeatureFolders().Returns(new Dictionary<string, string>());
        var plugin = new PlanningPlugin(_tempPath, _mockMapper, _retryPolicy);

        // Act
        var result = await plugin.ListPlanningDocsAsync("missing");

        // Assert
        Assert.Contains("Error", result);
    }

    [Fact]
    public async Task ReadPlanningDoc_ReturnsContent_WhenFileExists()
    {
        // Arrange
        CreateFeatureDir("feat1", "feature_feat1");
        CreatePlanningFile("feature_feat1", "DESIGN.md", "Hello Design");
        var plugin = new PlanningPlugin(_tempPath, _mockMapper, _retryPolicy);

        // Act
        var result = await plugin.ReadPlanningDocAsync("feat1", "DESIGN.md");

        // Assert
        Assert.Equal("Hello Design", result);
    }

    [Fact]
    public async Task SearchPlanningDocs_ReturnsMatches_WhenQueryMatches()
    {
        // Arrange
        CreateFeatureDir("feat1", "feature_feat1");
        CreatePlanningFile("feature_feat1", "STRATEGY.md", "This is the core requirement.");
        var plugin = new PlanningPlugin(_tempPath, _mockMapper, _retryPolicy);

        // Act
        // Note: This relies on 'rg' being installed on the local system (which we verified)
        var result = await plugin.SearchPlanningDocsAsync("feat1", "requirement");

        // Assert
        Assert.Contains("STRATEGY.md", result);
        Assert.Contains("requirement", result);
    }

    [Fact]
    public async Task SearchPlanningDocs_ReturnsNoMatches_WhenQueryDoesNotMatch()
    {
        // Arrange
        CreateFeatureDir("feat1", "feature_feat1");
        CreatePlanningFile("feature_feat1", "STRATEGY.md", "Nothing here.");
        var plugin = new PlanningPlugin(_tempPath, _mockMapper, _retryPolicy);

        // Act
        var result = await plugin.SearchPlanningDocsAsync("feat1", "missing_term");

        // Assert
        Assert.Contains("No matches found", result);
    }
}
