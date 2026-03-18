using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Google;
using System.Net.Http.Headers;
using InvestigatorAgent.Persistence;
using InvestigatorAgent.Configuration;

namespace InvestigatorAgent.Agent;

/// <summary>
/// Orchestrates the Investigator Agent conversation loop using Semantic Kernel.
/// Maintains conversation state across turns via <see cref="ChatHistory"/>.
/// </summary>
public sealed class AgentOrchestrator
{
    private readonly IChatCompletionService _chatService;
    private readonly ChatHistory _chatHistory;
    private readonly IConversationStore? _conversationStore;
    private readonly AgentSettings? _settings;
    private readonly string _conversationId = Guid.NewGuid().ToString("N");
    private readonly Kernel? _kernel;

    public AgentOrchestrator(Kernel kernel, IConversationStore? conversationStore = null, AgentSettings? settings = null)
    {
        _kernel = kernel;
        _chatService = kernel.GetRequiredService<IChatCompletionService>();
        _chatHistory = new ChatHistory();
        _chatHistory.AddSystemMessage(SystemPrompts.InvestigatorAgent);
        _conversationStore = conversationStore;
        _settings = settings;
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
    /// Static helper to build a Kernel configured for Google AI (Gemini).
    /// </summary>
    public static Kernel CreateGoogleKernel(string modelId, string apiKey)
    {
        var builder = Kernel.CreateBuilder();

#pragma warning disable SKEXP0070
        builder.AddGoogleAIGeminiChatCompletion(
            modelId: modelId,
            apiKey: apiKey
        );
#pragma warning restore SKEXP0070

        return builder.Build();
    }

    /// <summary>
    /// Initialises the orchestrator with a specific chat completion service.
    /// Intended for use in unit tests where the service is substituted.
    /// </summary>
    /// <param name="chatService">The chat completion service to use.</param>
    public AgentOrchestrator(IChatCompletionService chatService, IConversationStore? conversationStore = null, AgentSettings? settings = null)
    {
        _chatService = chatService;
        _chatHistory = new ChatHistory();
        _chatHistory.AddSystemMessage(SystemPrompts.InvestigatorAgent);
        _conversationStore = conversationStore;
        _settings = settings;
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

        PromptExecutionSettings? executionSettings = null;

        // Determine settings based on the service type
        if (_chatService.GetType().Name.Contains("Gemini"))
        {
            executionSettings = new GeminiPromptExecutionSettings
            {
                Temperature = (float)temperature,
                ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions
            };
        }
        else
        {
            // Default to OpenAI settings for OpenRouter/OpenAI
            executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = (float)temperature,
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };
        }

        var result = await _chatService.GetChatMessageContentsAsync(
            _chatHistory,
            executionSettings: executionSettings,
            kernel: _kernel
        );

        string response = result[0].Content ?? string.Empty;
        _chatHistory.AddAssistantMessage(response);

        if (_conversationStore != null && _settings != null)
        {
            await _conversationStore.SaveConversationAsync(_conversationId, _chatHistory, _settings);
        }

        return response;
    }

    /// <summary>
    /// Sends a user message to the agent and yields the response as a stream.
    /// Updates the conversation history with both the user message and the full agent response.
    /// </summary>
    /// <param name="userMessage">The message from the user.</param>
    /// <param name="temperature">The sampling temperature.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An asynchronous stream of the agent's response chunks.</returns>
    public async IAsyncEnumerable<string> SendMessageStreamAsync(
        string userMessage, 
        double temperature = 0.0, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _chatHistory.AddUserMessage(userMessage);

        PromptExecutionSettings? executionSettings = null;

        // Determine settings based on the service type
        if (_chatService.GetType().Name.Contains("Gemini"))
        {
            executionSettings = new GeminiPromptExecutionSettings
            {
                Temperature = (float)temperature,
                ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions
            };
        }
        else
        {
            // Default to OpenAI settings for OpenRouter/OpenAI
            executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = (float)temperature,
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };
        }

        string fullResponse = string.Empty;

        await foreach (var content in _chatService.GetStreamingChatMessageContentsAsync(
            _chatHistory,
            executionSettings: executionSettings,
            kernel: _kernel,
            cancellationToken: cancellationToken))
        {
            if (content.Content != null)
            {
                fullResponse += content.Content;
                yield return content.Content;
            }
        }

        _chatHistory.AddAssistantMessage(fullResponse);

        if (_conversationStore != null && _settings != null)
        {
            await _conversationStore.SaveConversationAsync(_conversationId, _chatHistory, _settings);
        }
    }

    /// <summary>
    /// Gets the full conversation history, including system, user, and assistant messages.
    /// </summary>
    public IReadOnlyList<ChatMessageContent> History => _chatHistory;
}
