using InvestigatorAgent.Configuration;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace InvestigatorAgent.Persistence;

/// <summary>
/// A conversation store that saves chat history to local JSON files in a specified directory.
/// </summary>
public sealed class FileConversationStore : IConversationStore
{
    private readonly string _outputDirectory;

    public FileConversationStore(string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
             throw new ArgumentException("Output directory cannot be null or empty.", nameof(outputDirectory));
        }

        _outputDirectory = Path.IsPathRooted(outputDirectory)
            ? outputDirectory
            : Path.Combine(Directory.GetCurrentDirectory(), outputDirectory);

        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }
    }

    public async Task SaveConversationAsync(string conversationId, ChatHistory history, AgentSettings settings)
    {
        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        string filename = $"conv_{timestamp}_{conversationId}.json";
        string filePath = Path.Combine(_outputDirectory, filename);

        var data = new
        {
            ConversationId = conversationId,
            Model = settings.ModelName,
            Provider = !string.IsNullOrWhiteSpace(settings.GoogleApiKey) ? "Google" : "OpenRouter",
            MessageCount = history.Count,
            Messages = history.Select(m => new 
            { 
                Role = m.Role.Label, 
                Content = m.Content 
            }).ToList()
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(data, options);

        await File.WriteAllTextAsync(filePath, json);
    }
}
