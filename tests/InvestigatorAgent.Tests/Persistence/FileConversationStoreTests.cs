using InvestigatorAgent.Configuration;
using InvestigatorAgent.Persistence;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using Xunit;

namespace InvestigatorAgent.Tests.Persistence;

public class FileConversationStoreTests : IDisposable
{
    private readonly string _testDirectory;

    public FileConversationStoreTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"Conversations_{Guid.NewGuid()}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task SaveConversationAsync_CreatesJsonFileWithCorrectContent()
    {
        // Arrange
        var store = new FileConversationStore(_testDirectory);
        var conversationId = "test-conv-123";
        var history = new ChatHistory();
        history.AddSystemMessage("You are a system");
        history.AddUserMessage("Hello");
        var settings = new AgentSettings { ModelName = "test-model-abc", Temperature = 0.0 };

        // Act
        await store.SaveConversationAsync(conversationId, history, settings);

        // Assert
        var files = Directory.GetFiles(_testDirectory, "conv_*.json");
        Assert.Single(files);
        
        var jsonContent = await File.ReadAllTextAsync(files[0]);
        var jsonDoc = JsonDocument.Parse(jsonContent);
        var root = jsonDoc.RootElement;
        
        Assert.Equal(conversationId, root.GetProperty("ConversationId").GetString());
        Assert.Equal("test-model-abc", root.GetProperty("Model").GetString());
        Assert.Equal("OpenRouter", root.GetProperty("Provider").GetString());
        Assert.Equal(2, root.GetProperty("MessageCount").GetInt32());
        
        var messages = root.GetProperty("Messages");
        Assert.Equal(2, messages.GetArrayLength());
        Assert.Equal("system", messages[0].GetProperty("Role").GetString());
        Assert.Equal("You are a system", messages[0].GetProperty("Content").GetString());
    }
}
