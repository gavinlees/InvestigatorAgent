using FluentAssertions;
using InvestigatorAgent.Plugins;
using InvestigatorAgent.Utils;
using System.IO;

namespace InvestigatorAgent.Tests.Plugins;

public class PlanningPluginTests : IDisposable
{
    private readonly string _tempPath;

    public PlanningPluginTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
        {
            Directory.Delete(_tempPath, true);
        }
    }

    [Fact]
    public async Task ListPlanningDocsAsync_ReturnsFiles()
    {
        // Arrange
        var featureDir = Path.Combine(_tempPath, "feature1");
        var planningDir = Path.Combine(featureDir, "planning");
        Directory.CreateDirectory(planningDir);
        
        File.WriteAllText(Path.Combine(planningDir, "USER_STORY.md"), "content");
        File.WriteAllText(Path.Combine(planningDir, "DESIGN.md"), "content");

        var mapper = new FeatureFolderMapper(_tempPath);
        var plugin = new PlanningPlugin(_tempPath, mapper);

        // Act
        string result = await plugin.ListPlanningDocsAsync("feature1");

        // Assert
        result.Should().Contain("USER_STORY.md");
        result.Should().Contain("DESIGN.md");
    }

    [Fact]
    public async Task ReadPlanningDocAsync_ReturnsContent()
    {
        // Arrange
        var featureDir = Path.Combine(_tempPath, "feature1");
        var planningDir = Path.Combine(featureDir, "planning");
        Directory.CreateDirectory(planningDir);
        
        string expectedContent = "# User Story\nAs a user...";
        File.WriteAllText(Path.Combine(planningDir, "USER_STORY.md"), expectedContent);

        var mapper = new FeatureFolderMapper(_tempPath);
        var plugin = new PlanningPlugin(_tempPath, mapper);

        // Act
        string result = await plugin.ReadPlanningDocAsync("feature1", "USER_STORY.md");

        // Assert
        result.Should().Be(expectedContent);
    }

    [Fact]
    public async Task SearchPlanningDocsAsync_ReturnsMatchingLines()
    {
        // Arrange
        var featureDir = Path.Combine(_tempPath, "feature1");
        var planningDir = Path.Combine(featureDir, "planning");
        Directory.CreateDirectory(planningDir);
        
        File.WriteAllText(Path.Combine(planningDir, "DOC1.md"), "Line 1: Authentication is key\nLine 2: Other stuff");
        File.WriteAllText(Path.Combine(planningDir, "DOC2.md"), "Line 1: More on Auth\nLine 2: No match here");

        var mapper = new FeatureFolderMapper(_tempPath);
        var plugin = new PlanningPlugin(_tempPath, mapper);

        // Act
        string result = await plugin.SearchPlanningDocsAsync("feature1", "Auth");

        // Assert
        result.Should().Contain("DOC1.md [Line 1]: Line 1: Authentication is key");
        result.Should().Contain("DOC2.md [Line 1]: Line 1: More on Auth");
        result.Should().NotContain("No match here");
    }

    [Fact]
    public async Task SearchPlanningDocsAsync_NoMatches_ReturnsMessage()
    {
        // Arrange
        var featureDir = Path.Combine(_tempPath, "feature1");
        var planningDir = Path.Combine(featureDir, "planning");
        Directory.CreateDirectory(planningDir);
        File.WriteAllText(Path.Combine(planningDir, "DOC1.md"), "Some content");

        var mapper = new FeatureFolderMapper(_tempPath);
        var plugin = new PlanningPlugin(_tempPath, mapper);

        // Act
        string result = await plugin.SearchPlanningDocsAsync("feature1", "MissingTerm");

        // Assert
        result.Should().Contain("No matches found for 'MissingTerm'");
    }
}
