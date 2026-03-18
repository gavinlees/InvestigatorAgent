using FluentAssertions;
using InvestigatorAgent.Plugins;
using InvestigatorAgent.Utils;
using System.Text.Json;

namespace InvestigatorAgent.Tests.Plugins;

public class AnalysisPluginTests : IDisposable
{
    private readonly string _tempPath;

    public AnalysisPluginTests()
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
    public async Task GetAnalysisAsync_ValidUnitTestType_ReturnsJson()
    {
        // Arrange
        var featureDir = Path.Combine(_tempPath, "feature1");
        var metricsDir = Path.Combine(featureDir, "metrics");
        Directory.CreateDirectory(metricsDir);
        
        var jsonContent = "{ \"tests_passed\": 10, \"tests_failed\": 0 }";
        await File.WriteAllTextAsync(Path.Combine(metricsDir, "unit_test_results.json"), jsonContent);

        var mapper = new FeatureFolderMapper(_tempPath);
        var plugin = new AnalysisPlugin(_tempPath, mapper);

        // Act
        string resultJson = await plugin.GetAnalysisAsync("feature1", "metrics/unit_test_results");

        // Assert
        resultJson.Should().Be(jsonContent);
    }

    [Fact]
    public async Task GetAnalysisAsync_ValidCoverageType_ReturnsJson()
    {
        // Arrange
        var featureDir = Path.Combine(_tempPath, "feature2");
        var metricsDir = Path.Combine(featureDir, "metrics");
        Directory.CreateDirectory(metricsDir);
        
        var jsonContent = "{ \"overall_coverage\": 85 }";
        await File.WriteAllTextAsync(Path.Combine(metricsDir, "test_coverage_report.json"), jsonContent);

        var mapper = new FeatureFolderMapper(_tempPath);
        var plugin = new AnalysisPlugin(_tempPath, mapper);

        // Act
        string resultJson = await plugin.GetAnalysisAsync("feature2", "metrics/test_coverage_report");

        // Assert
        resultJson.Should().Be(jsonContent);
    }

    [Fact]
    public async Task GetAnalysisAsync_InvalidAnalysisType_ReturnsError()
    {
        // Arrange
        var mapper = new FeatureFolderMapper(_tempPath);
        var plugin = new AnalysisPlugin(_tempPath, mapper);

        // Act
        string result = await plugin.GetAnalysisAsync("feature1", "metrics/unsupported_report");

        // Assert
        result.Should().StartWith("Error: Unsupported analysis type");
    }

    [Fact]
    public async Task GetAnalysisAsync_InvalidFeatureId_ReturnsError()
    {
        // Arrange
        var mapper = new FeatureFolderMapper(_tempPath);
        var plugin = new AnalysisPlugin(_tempPath, mapper);

        // Act
        string result = await plugin.GetAnalysisAsync("unknown_feature", "metrics/unit_test_results");

        // Assert
        result.Should().StartWith("Error: Feature ID 'unknown_feature' not found.");
    }

    [Fact]
    public async Task GetAnalysisAsync_MissingFile_ReturnsError()
    {
        // Arrange
        var featureDir = Path.Combine(_tempPath, "feature1");
        Directory.CreateDirectory(featureDir); // Feature exists, but metrics dir/file doesn't

        var mapper = new FeatureFolderMapper(_tempPath);
        var plugin = new AnalysisPlugin(_tempPath, mapper);

        // Act
        string result = await plugin.GetAnalysisAsync("feature1", "metrics/unit_test_results");

        // Assert
        result.Should().StartWith("Error: Analysis file 'metrics/unit_test_results' not found");
    }
}
