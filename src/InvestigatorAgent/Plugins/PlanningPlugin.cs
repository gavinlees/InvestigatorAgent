using InvestigatorAgent.Utils;
using InvestigatorAgent.Resilience;
using InvestigatorAgent.Observability;
using Microsoft.SemanticKernel;
using Polly.Retry;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

namespace InvestigatorAgent.Plugins;

/// <summary>
/// A Semantic Kernel plugin that provides access to planning documents
/// stored in the local file system.
/// </summary>
public sealed class PlanningPlugin
{
    private readonly string _dataDirectory;
    private readonly IFeatureFolderMapper _mapper;
    private readonly AsyncRetryPolicy _retryPolicy;

    public PlanningPlugin(string dataDirectory, IFeatureFolderMapper mapper, AsyncRetryPolicy? retryPolicy = null)
    {
        _dataDirectory = dataDirectory ?? throw new ArgumentNullException(nameof(dataDirectory));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _retryPolicy = retryPolicy ?? RetryPolicies.CreateToolRetryPolicy(new RetryConfiguration());
    }

    /// <summary>
    /// Lists all planning documents available for a specific feature.
    /// </summary>
    [KernelFunction("list_planning_docs")]
    [Description("Lists all planning documents available for a specific feature. Use this to see what documentation exists.")]
    public async Task<string> ListPlanningDocsAsync(
        [Description("The feature ID (e.g., 'feature1')")] string featureId)
    {
        using var activity = TelemetrySetup.Source.StartActivity("tool.list_planning_docs");
        activity?.SetTag("feature_id", featureId);

        var folders = _mapper.GetFeatureFolders();
        if (!folders.TryGetValue(featureId, out string? folderPath))
        {
            return $"Error: Feature ID '{featureId}' not found.";
        }

        string planningDir = Path.Combine(folderPath, "planning");
        if (!Directory.Exists(planningDir))
        {
            return $"No planning documents found for feature '{featureId}'.";
        }

        try
        {
            var files = Directory.GetFiles(planningDir, "*.md")
                                .Select(Path.GetFileName)
                                .ToList();
            
            activity?.SetTag("doc_count", files.Count);

            if (files.Count == 0)
            {
                return $"No planning documents found for feature '{featureId}'.";
            }

            return string.Join(", ", files);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return $"Error: Failed to list planning documents: {ex.Message}";
        }
    }

    /// <summary>
    /// Reads a specific planning document. 
    /// WARNING: Planning documents can be large. Use search_planning_docs if you are looking for specific info.
    /// </summary>
    [KernelFunction("read_planning_doc")]
    [Description("Reads a specific planning document. WARNING: Docs may be large (10-25KB). Use search_planning_docs for targeted info.")]
    public async Task<string> ReadPlanningDocAsync(
        [Description("The feature ID")] string featureId,
        [Description("The filename (e.g., 'USER_STORY.md')")] string docName)
    {
        using var activity = TelemetrySetup.Source.StartActivity("tool.read_planning_doc");
        activity?.SetTag("feature_id", featureId);
        activity?.SetTag("doc_name", docName);

        var folders = _mapper.GetFeatureFolders();
        if (!folders.TryGetValue(featureId, out string? folderPath))
        {
            return $"Error: Feature ID '{featureId}' not found.";
        }

        string filePath = Path.Combine(folderPath, "planning", docName);
        if (!File.Exists(filePath))
        {
            return $"Error: Document '{docName}' not found for feature '{featureId}'.";
        }

        try
        {
            string content = await _retryPolicy.ExecuteAsync(async () => await File.ReadAllTextAsync(filePath));
            activity?.SetTag("content_length", content.Length);
            return content;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return $"Error: Failed to read planning document: {ex.Message}";
        }
    }

    /// <summary>
    /// Searches planning documents for a specific term or regex.
    /// </summary>
    [KernelFunction("search_planning_docs")]
    [Description("Searches all planning documents for a feature for a specific query using ripgrep. Returns matching lines with context.")]
    public async Task<string> SearchPlanningDocsAsync(
        [Description("The feature ID")] string featureId,
        [Description("The search query (term or simple regex)")] string query)
    {
        using var activity = TelemetrySetup.Source.StartActivity("tool.search_planning_docs");
        activity?.SetTag("feature_id", featureId);
        activity?.SetTag("search_query", query);

        var folders = _mapper.GetFeatureFolders();
        if (!folders.TryGetValue(featureId, out string? folderPath))
        {
            return $"Error: Feature ID '{featureId}' not found.";
        }

        string planningDir = Path.Combine(folderPath, "planning");
        if (!Directory.Exists(planningDir))
        {
            return $"No planning documents found for feature '{featureId}'.";
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "rg",
                Arguments = $"--json \"{query}\" \"{planningDir}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Failed to start ripgrep process.");
                return "Error: Failed to start ripgrep process.";
            }

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
            {
                activity?.SetStatus(ActivityStatusCode.Error, $"ripgrep failed: {error}");
                return $"Error: ripgrep failed with exit code {process.ExitCode}: {error}";
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                activity?.SetTag("match_count", 0);
                return $"No matches found for '{query}' in planning documents of feature '{featureId}'.";
            }

            var results = new List<string>();
            var lines = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                if (root.GetProperty("type").GetString() == "match")
                {
                    var data = root.GetProperty("data");
                    string fileName = Path.GetFileName(data.GetProperty("path").GetProperty("text").GetString() ?? "unknown");
                    int lineNumber = data.GetProperty("line_number").GetInt32();
                    string matchText = data.GetProperty("lines").GetProperty("text").GetString()?.Trim() ?? string.Empty;

                    results.Add($"{fileName} [Line {lineNumber}]: {matchText}");
                    if (results.Count >= 20) break;
                }
            }

            activity?.SetTag("match_count", results.Count);

            if (results.Count == 0)
            {
                return $"No matches found for '{query}' in planning documents of feature '{featureId}'.";
            }

            return string.Join("\n", results);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return $"Error: Search failed: {ex.Message}";
        }
    }
}
