namespace InvestigatorAgent.Configuration;

/// <summary>
/// Strongly-typed configuration record for the Investigator Agent.
/// All required fields must be present; optional fields default to null.
/// </summary>
public record AgentSettings
{
    /// <summary>Gets the optional OpenRouter API key used to authenticate LLM requests.</summary>
    public string? OpenRouterApiKey { get; init; }

    /// <summary>Gets the Google API key used to authenticate LLM requests (Direct Gemini API).</summary>
    public string? GoogleApiKey { get; init; }

    /// <summary>Gets the model identifier to use for LLM completions (e.g., gemini-1.5-flash).</summary>
    public required string ModelName { get; init; }

    /// <summary>Gets the sampling temperature for LLM responses (0.0 to 1.0).</summary>
    public required double Temperature { get; init; }

    /// <summary>Gets the optional maximum number of tokens for each LLM response.</summary>
    public int? MaxTokens { get; init; }

    /// <summary>Gets the optional directory path for storing trace output files.</summary>
    public string? TraceOutputDir { get; init; }

    /// <summary>Gets the optional directory path for storing persisted conversation files.</summary>
    public string? ConversationOutputDir { get; init; }

    /// <summary>Gets the optional directory path for the incoming feature data.</summary>
    public string? DataDirectory { get; init; }
}
