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

    public LangfuseClient(string baseUrl, string publicKey, string secretKey)
    {
        _publicKey = publicKey;
        _secretKey = secretKey;
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        
        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{publicKey}:{secretKey}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
    }

    public async Task CreateOrUpdateDatasetAsync(string name, string description)
    {
        var payload = new { name, description };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        
        // v2 API uses datasets endpoint
        var response = await _httpClient.PostAsync("/api/public/v2/datasets", content);
        // We ignore 409 (Conflict) if it already exists, or we could use GET to check
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.Conflict)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Warning: Failed to create dataset: {error}");
        }
    }

    public async Task AddDatasetItemAsync(string datasetName, string input, string expectedOutput, object? metadata = null)
    {
        var payload = new 
        { 
            datasetName, 
            input = new { text = input }, 
            expectedOutput = new { text = expectedOutput },
            metadata
        };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/api/public/v2/dataset-items", content);
        if (!response.IsSuccessStatusCode)
        {
            // Logging but not failing to allow multiple runs
        }
    }

    public async Task LogDatasetRunItemAsync(string runName, string datasetItemId, string traceId, string output)
    {
        var payload = new 
        { 
            runName, 
            datasetItemId, 
            traceId, 
            output = new { text = output } 
        };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        await _httpClient.PostAsync("/api/public/v2/dataset-run-items", content);
    }

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
        await _httpClient.PostAsync("/api/public/v2/scores", content);
    }

    public void Dispose() => _httpClient.Dispose();
}
