using InvestigatorAgent.Utils;
using InvestigatorAgent.Resilience;
using Microsoft.SemanticKernel;
using Polly.Retry;

namespace InvestigatorAgent.Plugins;

/// <summary>
/// A Semantic Kernel plugin that provides access to analysis metrics and reports
/// stored in the local file system.
/// </summary>
public sealed class AnalysisPlugin
{
    private readonly string _dataDirectory;
    private readonly IFeatureFolderMapper _mapper;
    private readonly AsyncRetryPolicy _retryPolicy;

    public AnalysisPlugin(string dataDirectory, IFeatureFolderMapper mapper, AsyncRetryPolicy? retryPolicy = null)
    {
        _dataDirectory = dataDirectory ?? throw new ArgumentNullException(nameof(dataDirectory));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _retryPolicy = retryPolicy ?? RetryPolicies.CreateToolRetryPolicy(new RetryConfiguration());
    }

    private static readonly HashSet<string> ValidAnalysisTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "metrics/unit_test_results",
        "metrics/test_coverage_report",
        "metrics/pipeline_results",
        "metrics/performance_benchmarks",
        "metrics/security_scan_results",
        "reviews/security",
        "reviews/uat",
        "reviews/stakeholders"
    };

    /// <summary>
    /// Retrieves analysis data for a specific feature.
    /// Supported analysis types: 
    /// 'metrics/unit_test_results', 
    /// 'metrics/test_coverage_report',
    /// 'metrics/pipeline_results',
    /// 'metrics/performance_benchmarks',
    /// 'metrics/security_scan_results',
    /// 'reviews/security',
    /// 'reviews/uat',
    /// 'reviews/stakeholders'.
    /// </summary>
    [KernelFunction("get_analysis")]
    [System.ComponentModel.Description("Retrieves analysis data for a specific feature. Requires feature_id and analysis_type parameters.")]
    public async Task<string> GetAnalysisAsync(
        [System.ComponentModel.Description("The feature ID (e.g., 'feature1', 'feature2')")] string featureId,
        [System.ComponentModel.Description("Analysis type: 'metrics/unit_test_results', 'metrics/test_coverage_report', 'metrics/pipeline_results', 'metrics/performance_benchmarks', 'metrics/security_scan_results', 'reviews/security', 'reviews/uat', or 'reviews/stakeholders'")] string analysisType)
    {
        if (!ValidAnalysisTypes.Contains(analysisType))
        {
            return $"Error: Unsupported analysis type '{analysisType}'. Available types are: {string.Join(", ", ValidAnalysisTypes)}.";
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
            string json = await _retryPolicy.ExecuteAsync(async () => await File.ReadAllTextAsync(filePath));
            return json;
        }
        catch (Exception ex)
        {
            return $"Error: Failed to read analysis file: {ex.Message}";
        }
    }
}
