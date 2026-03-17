using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Net.Http.Headers;

namespace InvestigatorAgent.Agent;

/// <summary>
/// Orchestrates the Investigator Agent conversation loop using Semantic Kernel.
/// Maintains conversation state across turns via <see cref="ChatHistory"/>.
/// </summary>
public sealed class AgentOrchestrator
{
    private readonly IChatCompletionService _chatService;
    private readonly ChatHistory _chatHistory;

    public AgentOrchestrator(Kernel kernel)
    {
        _chatService = kernel.GetRequiredService<IChatCompletionService>();
        _chatHistory = new ChatHistory();
        _chatHistory.AddSystemMessage(SystemPrompts.InvestigatorAgent);
    }

    /// <summary>
    /// Static helper to build a Kernel configured for OpenRouter.
    /// Injects required headers to avoid 400 Forbidden/Bad Request.
    /// </summary>
    public static Kernel CreateOpenRouterKernel(string modelId, string apiKey)
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/semantic-kernel-investigator");
        httpClient.DefaultRequestHeaders.Add("X-Title", "Investigator Agent");

        var builder = Kernel.CreateBuilder();

#pragma warning disable SKEXP0010
        builder.AddOpenAIChatCompletion(
            modelId: modelId,
            apiKey: apiKey,
            endpoint: new Uri("https://openrouter.ai/api/v1"),
            httpClient: httpClient
        );
#pragma warning restore SKEXP0010

        return builder.Build();
    }

    /// <summary>
    /// Initialises the orchestrator with a specific chat completion service.
    /// Intended for use in unit tests where the service is substituted.
    /// </summary>
    /// <param name="chatService">The chat completion service to use.</param>
    public AgentOrchestrator(IChatCompletionService chatService)
    {
        _chatService = chatService;
        _chatHistory = new ChatHistory();
        _chatHistory.AddSystemMessage(SystemPrompts.InvestigatorAgent);
    }

    /// <summary>
    /// Sends a user message to the agent and returns the agent's response.
    /// Updates the conversation history with both the user message and the agent response.
    /// </summary>
    /// <param name="userMessage">The message from the user.</param>
    /// <returns>The agent's response content.</returns>
    public async Task<string> SendMessageAsync(string userMessage, double temperature = 0.0)
    {
        _chatHistory.AddUserMessage(userMessage);

        OpenAIPromptExecutionSettings executionSettings = new()
        {
             Temperature = (float)temperature
        };

        var result = await _chatService.GetChatMessageContentsAsync(
            _chatHistory,
            executionSettings: executionSettings
        );

        string response = result[0].Content ?? string.Empty;
        _chatHistory.AddAssistantMessage(response);

        return response;
    }

    /// <summary>
    /// Gets the full conversation history, including system, user, and assistant messages.
    /// </summary>
    public IReadOnlyList<ChatMessageContent> History => _chatHistory;
}
