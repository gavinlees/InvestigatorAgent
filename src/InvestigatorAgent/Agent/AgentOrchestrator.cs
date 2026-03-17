using InvestigatorAgent.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace InvestigatorAgent.Agent;

/// <summary>
/// Orchestrates the Investigator Agent conversation loop using Semantic Kernel.
/// Maintains conversation state across turns via <see cref="ChatHistory"/>.
/// </summary>
public sealed class AgentOrchestrator
{
    private readonly IChatCompletionService _chatService;
    private readonly ChatHistory _chatHistory;

    /// <summary>
    /// Initialises the orchestrator with a configured Semantic Kernel instance.
    /// Adds the system prompt to the conversation history.
    /// </summary>
    /// <param name="kernel">A configured Semantic Kernel instance.</param>
    public AgentOrchestrator(Kernel kernel)
    {
        _chatService = kernel.GetRequiredService<IChatCompletionService>();
        _chatHistory = new ChatHistory();
        _chatHistory.AddSystemMessage(SystemPrompts.InvestigatorAgent);
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
    public async Task<string> SendMessageAsync(string userMessage)
    {
        _chatHistory.AddUserMessage(userMessage);

        IReadOnlyList<ChatMessageContent> result = await _chatService.GetChatMessageContentsAsync(
            _chatHistory
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
