using InvestigatorAgent.Utils;
using InvestigatorAgent.Resilience;
using Microsoft.SemanticKernel;
using Polly.Retry;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InvestigatorAgent.Plugins;

/// <summary>
/// A Semantic Kernel plugin that provides access to Jira feature metadata
/// stored in the local file system.
/// </summary>
public sealed class JiraPlugin
{
    private readonly FeatureFolderMapper _mapper;
    private readonly AsyncRetryPolicy _retryPolicy;

    public JiraPlugin(FeatureFolderMapper mapper, AsyncRetryPolicy? retryPolicy = null)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _retryPolicy = retryPolicy ?? RetryPolicies.CreateToolRetryPolicy(new RetryConfiguration());
    }

    /// <summary>
    /// Retrieves a high-level summary of all features being tracked.
    /// Returns a JSON string containing an array of feature objects with their ID, Jira Key, Summary, and Status.
    /// </summary>
    [KernelFunction("get_jira_data")]
    [System.ComponentModel.Description("Retrieves a high-level summary of all features being tracked. Returns a JSON string containing an array of feature objects with their ID, Jira Key, Summary, and Status. Use this first when a user asks about a feature.")]
    public async Task<string> GetJiraDataAsync()
    {
        var folders = _mapper.GetFeatureFolders();
        var results = new List<FeatureSummary>();

        foreach (var kvp in folders)
        {
            string featureId = kvp.Key;
            string folderPath = kvp.Value;
            string jiraFilePath = Path.Combine(folderPath, "jira", "feature_issue.json");

            if (File.Exists(jiraFilePath))
            {
                try
                {
                    string json = await _retryPolicy.ExecuteAsync(async () => await File.ReadAllTextAsync(jiraFilePath));
                    var issueOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var issueDocument = JsonSerializer.Deserialize<JiraIssueDocument>(json, issueOptions);

                    if (issueDocument != null)
                    {
                        results.Add(new FeatureSummary
                        {
                            Id = featureId,
                            Key = issueDocument.Key ?? "Unknown",
                            Summary = issueDocument.Fields?.Summary ?? "Unknown",
                            Status = issueDocument.Fields?.Status?.Name ?? "Unknown"
                        });
                    }
                }
                catch (Exception ex)
                {
                    // In a production app, we would log this. For the agent, we can optionally include failed reads.
                    results.Add(new FeatureSummary
                    {
                        Id = featureId,
                        Key = "Error",
                        Summary = $"Failed to read data: {ex.Message}",
                        Status = "Error"
                    });
                }
            }
            else
            {
                results.Add(new FeatureSummary
                {
                    Id = featureId,
                    Key = "Missing",
                    Summary = "No Jira data found",
                    Status = "Unknown"
                });
            }
        }

        return JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
    }

    // --- Data Transfer Objects ---

    private sealed record FeatureSummary
    {
        [JsonPropertyName("id")]
        public required string Id { get; init; }

        [JsonPropertyName("key")]
        public required string Key { get; init; }

        [JsonPropertyName("summary")]
        public required string Summary { get; init; }

        [JsonPropertyName("status")]
        public required string Status { get; init; }
    }

    private sealed record JiraIssueDocument
    {
        public string? Key { get; init; }
        public JiraIssueFields? Fields { get; init; }
    }

    private sealed record JiraIssueFields
    {
        public string? Summary { get; init; }
        public JiraIssueStatus? Status { get; init; }
    }

    private sealed record JiraIssueStatus
    {
        public string? Name { get; init; }
    }
}
