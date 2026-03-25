using FluentAssertions;
using InvestigatorAgent.Configuration;

namespace InvestigatorAgent.Tests.Configuration;

/// <summary>
/// Unit tests for <see cref="AgentSettings"/> record and <see cref="ConfigurationLoader"/>.
/// </summary>
public sealed class AgentSettingsTests : IDisposable
{
    private readonly List<string> _setKeys = [];
    private static readonly string[] AllConfigKeys = 
    [
        "OPENROUTER_API_KEY", "GOOGLE_API_KEY", "MODEL_NAME", "TEMPERATURE", 
        "MAX_TOKENS", "TRACE_OUTPUT_DIR", "CONVERSATION_OUTPUT_DIR", 
        "DATA_DIRECTORY", "GRAPHITI_MCP_URL", "LANGFUSE_PUBLIC_KEY", 
        "LANGFUSE_SECRET_KEY", "LANGFUSE_BASE_URL", "CONVERSATION_SUMMARY_THRESHOLD", 
        "CONVERSATION_SUMMARY_REMAINING", "MAX_RETRY_ATTEMPTS"
    ];

    public AgentSettingsTests()
    {
        ClearAllEnv();
    }

    private void ClearAllEnv()
    {
        foreach (var key in AllConfigKeys)
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    /// <summary>
    /// Verifies that settings load successfully when all required environment variables are present.
    /// </summary>
    [Fact]
    public void Load_WithOpenRouterApiKey_ReturnsPopulatedSettings()
    {
        // Arrange
        SetEnv("OPENROUTER_API_KEY", "test-key");
        SetEnv("MODEL_NAME", "openai/gpt-4o-mini");
        SetEnv("TEMPERATURE", "0.5");

        // Act
        AgentSettings settings = ConfigurationLoader.Load();

        // Assert
        settings.OpenRouterApiKey.Should().Be("test-key");
        settings.ModelName.Should().Be("openai/gpt-4o-mini");
        settings.Temperature.Should().Be(0.5);
    }

    /// <summary>
    /// Verifies that settings load successfully when Google API key is present.
    /// </summary>
    [Fact]
    public void Load_WithGoogleApiKey_ReturnsPopulatedSettings()
    {
        // Arrange
        SetEnv("GOOGLE_API_KEY", "google-test-key");
        SetEnv("MODEL_NAME", "gemini-1.5-flash");
        SetEnv("TEMPERATURE", "0.5");

        // Act
        AgentSettings settings = ConfigurationLoader.Load();

        // Assert
        settings.GoogleApiKey.Should().Be("google-test-key");
        settings.ModelName.Should().Be("gemini-1.5-flash");
        settings.Temperature.Should().Be(0.5);
    }

    /// <summary>
    /// Verifies that an optional MAX_TOKENS variable is correctly parsed when present.
    /// </summary>
    [Fact]
    public void Load_WithOptionalMaxTokens_ParsesCorrectly()
    {
        // Arrange
        SetEnv("OPENROUTER_API_KEY", "test-key");
        SetEnv("MODEL_NAME", "openai/gpt-4o-mini");
        SetEnv("TEMPERATURE", "0.0");
        SetEnv("MAX_TOKENS", "2048");

        // Act
        AgentSettings settings = ConfigurationLoader.Load();

        // Assert
        settings.MaxTokens.Should().Be(2048);
    }

    /// <summary>
    /// Verifies that optional fields default to null when absent.
    /// </summary>
    [Fact]
    public void Load_WithOptionalFieldsAbsent_DefaultsToNull()
    {
        // Arrange
        SetEnv("OPENROUTER_API_KEY", "test-key");
        SetEnv("MODEL_NAME", "openai/gpt-4o-mini");
        SetEnv("TEMPERATURE", "0.0");

        // Act
        AgentSettings settings = ConfigurationLoader.Load();

        // Assert
        settings.MaxTokens.Should().BeNull();
        settings.TraceOutputDir.Should().BeNull();
        settings.ConversationOutputDir.Should().BeNull();
        settings.DataDirectory.Should().BeNull();
        settings.LangfusePublicKey.Should().BeNull();
        settings.LangfuseSecretKey.Should().BeNull();
        settings.LangfuseBaseUrl.Should().BeNull();
    }

    /// <summary>
    /// Verifies that a missing OPENROUTER_API_KEY throws an InvalidOperationException with a helpful message.
    /// </summary>
    [Fact]
    public void Load_WithBothApiKeysMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        SetEnv("MODEL_NAME", "openai/gpt-4o-mini");
        SetEnv("TEMPERATURE", "0.0");
        // Neither key is set

        // Act
        Action act = () => ConfigurationLoader.Load();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*either 'OPENROUTER_API_KEY' or 'GOOGLE_API_KEY' must be present*");
    }

    /// <summary>
    /// Verifies that a missing MODEL_NAME throws an InvalidOperationException with a helpful message.
    /// </summary>
    [Fact]
    public void Load_WithMissingModelName_ThrowsInvalidOperationException()
    {
        // Arrange
        SetEnv("OPENROUTER_API_KEY", "test-key");
        SetEnv("TEMPERATURE", "0.0");
        // MODEL_NAME intentionally not set

        // Act
        Action act = () => ConfigurationLoader.Load();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MODEL_NAME*");
    }

    /// <summary>
    /// Verifies that an invalid TEMPERATURE value throws an InvalidOperationException.
    /// </summary>
    [Fact]
    public void Load_WithInvalidTemperature_ThrowsInvalidOperationException()
    {
        // Arrange
        SetEnv("OPENROUTER_API_KEY", "test-key");
        SetEnv("MODEL_NAME", "openai/gpt-4o-mini");
        SetEnv("TEMPERATURE", "not-a-number");

        // Act
        Action act = () => ConfigurationLoader.Load();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TEMPERATURE*");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void SetEnv(string key, string value)
    {
        Environment.SetEnvironmentVariable(key, value);
        _setKeys.Add(key);
    }

    /// <summary>Cleans up all environment variables set during the test.</summary>
    public void Dispose()
    {
        foreach (string key in _setKeys)
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }
}
