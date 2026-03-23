using InvestigatorAgent.Agent;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NSubstitute;
using Xunit;

namespace InvestigatorAgent.Tests.Agent;

public class ConversationSummaryServiceTests
{
    private readonly IChatCompletionService _mockChatService;
    private readonly ConversationSummaryService _service;

    public ConversationSummaryServiceTests()
    {
        _mockChatService = Substitute.For<IChatCompletionService>();
        _service = new ConversationSummaryService(_mockChatService);
    }

    [Fact]
    public async Task SummariseMessagesAsync_CallsChatService_WithCorrectPrompt()
    {
        // Arrange
        var messages = new List<ChatMessageContent>
        {
            new ChatMessageContent(AuthorRole.User, "Hello"),
            new ChatMessageContent(AuthorRole.Assistant, "Hi there")
        };

        var expectedSummary = "A brief greeting.";
        
        // Setup NSubstitute mock for IChatCompletionService
        _mockChatService.GetChatMessageContentsAsync(
            Arg.Any<ChatHistory>(),
            Arg.Any<PromptExecutionSettings>(),
            Arg.Any<Kernel>(),
            Arg.Any<CancellationToken>()
        ).Returns(new List<ChatMessageContent> { new ChatMessageContent(AuthorRole.Assistant, expectedSummary) });

        // Act
        var result = await _service.SummariseMessagesAsync(messages);

        // Assert
        Assert.Equal(expectedSummary, result);
        await _mockChatService.Received(1).GetChatMessageContentsAsync(
            Arg.Is<ChatHistory>(h => h.Any(m => m.Content!.Contains("Hello") && m.Content!.Contains("Hi there"))),
            Arg.Any<PromptExecutionSettings>(),
            Arg.Any<Kernel>(),
            Arg.Any<CancellationToken>()
        );
    }
}
