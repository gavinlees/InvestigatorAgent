using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace InvestigatorAgent.Evaluation;

/// <summary>
/// A lightweight client for interacting with the Langfuse REST API (v2).
/// </summary>
public sealed class LangfuseClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _publicKey;
    private readonly string _secretKey;

    /// <summary>
    /// Initialises a new instance of the <see cref="LangfuseClient"/> class.
    /// </summary>
    public LangfuseClient(string baseUrl, string publicKey, string secretKey)
    {
        _publicKey = publicKey;
        _secretKey = secretKey;
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        
        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{publicKey}:{secretKey}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
    }

    /// <summary>
    /// Creates a new dataset or updates an existing one in Langfuse.
    /// </summary>
    public async Task CreateOrUpdateDatasetAsync(string name, string description)
    {
        var payload = new { name, description };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        
        // Langfuse API v1/v2 usually supports /api/public/datasets
        var response = await _httpClient.PostAsync("/api/public/datasets", content);
        // We ignore 409 (Conflict) if it already exists, or we could use GET to check
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.Conflict)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Warning: Failed to create dataset: {response.StatusCode} - {error}");
        }
    }

    /// <summary>
    /// Adds a test scenario item to a Langfuse dataset.
    /// </summary>
    /// <returns>The ID of the created/updated item inside the dataset.</returns>
    public async Task<string?> AddDatasetItemAsync(string datasetName, string input, string expectedOutput, object? metadata = null)
    {
        var payload = new 
        { 
            datasetName, 
            input, 
            expectedOutput,
            metadata
        };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/api/public/dataset-items", content);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Warning: Failed to add dataset item: {response.StatusCode} - {error}");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
    }

    /// <summary>
    /// Links an agent execution trace to a specific dataset item as part of a named evaluation run.
    /// </summary>
    public async Task LogDatasetRunItemAsync(string runName, string datasetItemId, string traceId, string output)
    {
        var payload = new 
        { 
            runName, 
            datasetItemId, 
            traceId 
            // 'output' is not a recognized key in dataset-run-items v2 API
        };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        await _httpClient.PostAsync("/api/public/dataset-run-items", content);
    }

    /// <summary>
    /// Posts a score to an existing trace for evaluation purposes.
    /// </summary>
    public async Task PostScoreAsync(string traceId, string name, double value, string? comment = null)
    {
        var payload = new 
        { 
            traceId, 
            name, 
            value,
            comment
        };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/api/public/scores", content);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Warning: Failed to post score: {response.StatusCode} - {error}");
        }
    }

    public void Dispose() => _httpClient.Dispose();
}
