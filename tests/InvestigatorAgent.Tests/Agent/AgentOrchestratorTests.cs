using FluentAssertions;
using InvestigatorAgent.Agent;
using InvestigatorAgent.Configuration;
using InvestigatorAgent.Persistence;
using InvestigatorAgent.Resilience;
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

    /// <summary>
    /// Verifies that the conversation is saved to the store after a message is sent.
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_SavesConversation_WhenStoreIsProvided()
    {
        // Arrange
        var conversationStoreMock = Substitute.For<IConversationStore>();
        var settings = new AgentSettings { ModelName = "test-model", Temperature = 0.0, Retry = new RetryConfiguration() };
        var orchestratorWithStore = new AgentOrchestrator(_chatService, conversationStoreMock, settings);
        
        SetupMockResponse("I will save this.");

        // Act
        await orchestratorWithStore.SendMessageAsync("Save please");

        // Assert
        await conversationStoreMock.Received(1).SaveConversationAsync(
            Arg.Any<string>(),
            Arg.Is<ChatHistory>(h => h.Count == 3), // System, User, Assistant
            settings);
    }

    /// <summary>
    /// Verifies that large tool results are truncated by the orchestrator.
    /// </summary>

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that summarisation is triggered when the message count exceeds the threshold.
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_TriggersSummarisation_WhenThresholdExceeded()
    {
        // Arrange
        var settings = new AgentSettings 
        { 
            ModelName = "test-model", 
            Temperature = 0.0, 
            Retry = new RetryConfiguration(),
            ConversationSummaryThreshold = 5, // [System, U1, A1, U2, A2] = 5. Next message triggers.
            ConversationSummaryRemaining = 2  // Keep last 2 (U2, A2)
        };
        var orchestrator = new AgentOrchestrator(_chatService, settings: settings);
        
        // Initial exchange (5 messages in total)
        SetupMockResponse("Response 1");
        await orchestrator.SendMessageAsync("Message 1");
        SetupMockResponse("Response 2");
        await orchestrator.SendMessageAsync("Message 2");
        orchestrator.History.Should().HaveCount(5);

        // Next exchange triggers summary
        // 1st call: Summary
        // 2nd call: Actual Response
        SetupMockResponse("Summary of stuff"); // For summary
        
        // Act
        // Current: [Sys, U1, A1, U2, A2]
        // SummariseHistoryIfNecessaryAsync called:
        //   toSummariseCount = 5 - 2 = 3.
        //   messagesToSummarise = Skip(1).Take(2) -> [U1, A1]
        //   Rebuild: [Sys, Summary, U2, A2] -> Count 4.
        // Then SendMessageAsync continues:
        //   AddUserMessage("Message 3") -> Count 5.
        //   GetChatMessageContentsAsync -> 6 turns? No, it's a loop.
        //   AddAssistantMessage -> Count 6.
        await orchestrator.SendMessageAsync("Message 3");

        // Assert: 
        // [System+Summary, U2, A2, U3, A3]
        orchestrator.History.Should().HaveCount(5);
        orchestrator.History[0].Role.Should().Be(AuthorRole.System);
        orchestrator.History[0].Content.Should().Contain("[CONVERSATION SUMMARY]");
        orchestrator.History[1].Role.Should().Be(AuthorRole.User); // Was U2
        orchestrator.History.Should().Contain(m => m.Content == "Message 3");
    }

    /// <summary>
    /// Verifies that summarisation is triggered and ensures an even history count (excluding system prompt).
    /// This prevents 400 Bad Request errors with Gemini when multiple User messages would otherwise be consecutive.
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_EnsuresEvenHistoryCount_WhenSummarising()
    {
        // Arrange
        var settings = new AgentSettings 
        { 
            ModelName = "test-model", 
            Temperature = 0.0, 
            Retry = new RetryConfiguration(),
            ConversationSummaryThreshold = 5, 
            ConversationSummaryRemaining = 3  // Leaving 3 would result in [U, A, U] -> BAD
        };
        var orchestrator = new AgentOrchestrator(_chatService, settings: settings);
        
        // Initial exchange (5 messages in total: S, U1, A1, U2, A2)
        SetupMockResponse("Response 1");
        await orchestrator.SendMessageAsync("Message 1");
        SetupMockResponse("Response 2");
        await orchestrator.SendMessageAsync("Message 2");
        
        orchestrator.History.Should().HaveCount(5);

        // Trigger summarisation with Message 3
        SetupMockResponse("Summary");   // For summary call
        SetupMockResponse("Response 3"); // For actual call
        
        // Act
        // Threshold 5 reached. Skip toSummariseCount (5-3=2). Index 2 is A1.
        // Loop skips A1, finds U2. 
        // Remaining count from U2 is 3 (U2, A2, U3? No, U3 isn't added yet). 
        // Wait, history is [S, U1, A1, U2, A2]. Count 5.
        // firstRecentIndex starts at 2 (A1).
        // - i=2: Role=A1. Skip.
        // - i=3: Role=U2. Remaining = 5 - 3 = 2. Role=User AND Remaining is Even. STOP.
        // Result: Keep [U2, A2]. 
        // Then SendMessageAsync adds Message 3.
        // Final: [S+Sum, U2, A2, U3, A3]. Count 5. (ENDS WITH ASSISTANT)
        await orchestrator.SendMessageAsync("Message 3");

        // Assert
        orchestrator.History.Should().HaveCount(5);
        orchestrator.History.Last().Role.Should().Be(AuthorRole.Assistant);
        orchestrator.History[1].Role.Should().Be(AuthorRole.User);
    }

    private void SetupMockResponse(string content)
    {
        ChatMessageContent message = new(AuthorRole.Assistant, content);
        _chatService
            .GetChatMessageContentsAsync(Arg.Any<ChatHistory>(), Arg.Any<PromptExecutionSettings>(), Arg.Any<Kernel>(), Arg.Any<CancellationToken>())
            .Returns([message]);
    }
}
