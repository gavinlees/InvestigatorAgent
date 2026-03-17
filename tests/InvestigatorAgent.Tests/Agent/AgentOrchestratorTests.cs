using FluentAssertions;
using InvestigatorAgent.Agent;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NSubstitute;

namespace InvestigatorAgent.Tests.Agent;

/// <summary>
/// Unit tests for <see cref="AgentOrchestrator"/> verifying conversation
/// history management and message routing without real LLM calls.
/// </summary>
public sealed class AgentOrchestratorTests
{
    private readonly IChatCompletionService _chatService;
    private readonly AgentOrchestrator _orchestrator;

    /// <summary>Initialises the test with a mocked chat completion service.</summary>
    public AgentOrchestratorTests()
    {
        _chatService = Substitute.For<IChatCompletionService>();
        _orchestrator = new AgentOrchestrator(_chatService);
    }

    /// <summary>
    /// Verifies that the system prompt is present in history upon initialisation.
    /// </summary>
    [Fact]
    public void Constructor_AddsSystemPromptToHistory()
    {
        // Assert
        _orchestrator.History.Should().ContainSingle(m =>
            m.Role == AuthorRole.System &&
            m.Content == SystemPrompts.InvestigatorAgent);
    }

    /// <summary>
    /// Verifies that SendMessageAsync returns the content from the LLM response.
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_ReturnsLlmResponseContent()
    {
        // Arrange
        string expectedResponse = "I help assess feature readiness.";
        SetupMockResponse(expectedResponse);

        // Act
        string result = await _orchestrator.SendMessageAsync("What do you do?");

        // Assert
        result.Should().Be(expectedResponse);
    }

    /// <summary>
    /// Verifies that the user message is added to history before the LLM call.
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_AddsUserMessageToHistory()
    {
        // Arrange
        SetupMockResponse("response");

        // Act
        await _orchestrator.SendMessageAsync("Hello there");

        // Assert
        _orchestrator.History.Should().Contain(m =>
            m.Role == AuthorRole.User &&
            m.Content == "Hello there");
    }

    /// <summary>
    /// Verifies that the assistant response is added to history after the LLM call.
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_AddsAssistantResponseToHistory()
    {
        // Arrange
        string agentReply = "I can help with that!";
        SetupMockResponse(agentReply);

        // Act
        await _orchestrator.SendMessageAsync("Hello");

        // Assert
        _orchestrator.History.Should().Contain(m =>
            m.Role == AuthorRole.Assistant &&
            m.Content == agentReply);
    }

    /// <summary>
    /// Verifies that history accumulates correctly across multiple turns.
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_AccumulatesHistoryAcrossMultipleTurns()
    {
        // Arrange
        SetupMockResponse("First response");
        await _orchestrator.SendMessageAsync("First message");

        SetupMockResponse("Second response");
        await _orchestrator.SendMessageAsync("Second message");

        // Assert: system + 2 user + 2 assistant = 5 entries
        _orchestrator.History.Should().HaveCount(5);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void SetupMockResponse(string content)
    {
        ChatMessageContent message = new(AuthorRole.Assistant, content);
        _chatService
            .GetChatMessageContentsAsync(Arg.Any<ChatHistory>(), Arg.Any<PromptExecutionSettings>(), Arg.Any<Kernel>(), Arg.Any<CancellationToken>())
            .Returns([message]);
    }
}
