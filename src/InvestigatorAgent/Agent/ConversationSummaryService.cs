using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;

namespace InvestigatorAgent.Agent;

/// <summary>
/// Service responsible for summarising conversation history to manage context window usage.
/// </summary>
public sealed class ConversationSummaryService
{
    private readonly IChatCompletionService _chatService;
    private readonly Kernel? _kernel;

    public ConversationSummaryService(IChatCompletionService chatService, Kernel? kernel = null)
    {
        _chatService = chatService;
        _kernel = kernel;
    }

    /// <summary>
    /// Summarises a collection of chat messages into a single string.
    /// </summary>
    /// <param name="messagesToSummarise">The history segment to condense.</param>
    /// <returns>A concise summary of the provided messages.</returns>
    public async Task<string> SummariseMessagesAsync(IEnumerable<ChatMessageContent> messagesToSummarise)
    {
        var history = new ChatHistory(SystemPrompts.ConversationSummarizer);
        
        var sb = new StringBuilder();
        foreach (var msg in messagesToSummarise)
        {
            sb.AppendLine($"{msg.Role}: {msg.Content}");
        }

        history.AddUserMessage($"Please summarise the following interaction:\n\n{sb}");

        var result = await _chatService.GetChatMessageContentsAsync(history, kernel: _kernel);
        return result[0].Content ?? "No summary generated.";
    }
}
