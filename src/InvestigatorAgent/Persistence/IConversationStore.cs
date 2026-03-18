using InvestigatorAgent.Configuration;
using Microsoft.SemanticKernel.ChatCompletion;

namespace InvestigatorAgent.Persistence;

/// <summary>
/// Defines an interface for saving conversation history and metadata.
/// </summary>
public interface IConversationStore
{
    /// <summary>
    /// Saves the provided conversation history and agent settings.
    /// </summary>
    /// <param name="conversationId">A unique identifier for the conversation session.</param>
    /// <param name="history">The history of messages exchanged.</param>
    /// <param name="settings">The current agent settings used for the conversation.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    Task SaveConversationAsync(string conversationId, ChatHistory history, AgentSettings settings);
}
