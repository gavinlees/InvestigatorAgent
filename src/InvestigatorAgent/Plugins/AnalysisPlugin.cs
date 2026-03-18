using InvestigatorAgent.Utils;
using Microsoft.SemanticKernel;

namespace InvestigatorAgent.Plugins;

/// <summary>
/// A Semantic Kernel plugin that provides access to analysis metrics and reports
/// stored in the local file system.
/// </summary>
public sealed class AnalysisPlugin
{
    private readonly string _dataDirectory;
    private readonly FeatureFolderMapper _mapper;

    public AnalysisPlugin(string dataDirectory, FeatureFolderMapper mapper)
    {
        _dataDirectory = dataDirectory ?? throw new ArgumentNullException(nameof(dataDirectory));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// Retrieves analysis data for a specific feature.
    /// Supported analysis types: 'metrics/unit_test_results', 'metrics/test_coverage_report'.
    /// </summary>
    [KernelFunction("get_analysis")]
    [System.ComponentModel.Description("Retrieves analysis data for a specific feature. Requires feature_id and analysis_type parameters.")]
    public async Task<string> GetAnalysisAsync(
        [System.ComponentModel.Description("The feature ID (e.g., 'feature1', 'feature2')")] string featureId,
        [System.ComponentModel.Description("Analysis type: 'metrics/unit_test_results' or 'metrics/test_coverage_report'")] string analysisType)
    {
        // For Step 3, we only support specific analysis types
        if (analysisType != "metrics/unit_test_results" && analysisType != "metrics/test_coverage_report")
        {
            return $"Error: Unsupported analysis type '{analysisType}'. Available types are: 'metrics/unit_test_results', 'metrics/test_coverage_report'.";
        }

        var folders = _mapper.GetFeatureFolders();
        if (!folders.TryGetValue(featureId, out string? folderPath))
        {
            return $"Error: Feature ID '{featureId}' not found.";
        }

        string filePath = Path.Combine(folderPath, $"{analysisType}.json");

        if (!File.Exists(filePath))
        {
            return $"Error: Analysis file '{analysisType}' not found for feature '{featureId}'.";
        }

        try
        {
            string json = await File.ReadAllTextAsync(filePath);
            return json;
        }
        catch (Exception ex)
        {
            return $"Error: Failed to read analysis file: {ex.Message}";
        }
    }
}
