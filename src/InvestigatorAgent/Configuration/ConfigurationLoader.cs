using dotenv.net;

namespace InvestigatorAgent.Configuration;

/// <summary>
/// Loads and validates agent configuration from environment variables,
/// populated from a .env file using dotenv.net.
/// </summary>
public static class ConfigurationLoader
{
    /// <summary>
    /// Loads environment variables from the .env file in the current working directory.
    /// Should be called once at application startup.
    /// </summary>
    public static void LoadEnv()
    {
        string envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (File.Exists(envPath))
        {
            DotEnv.Load(options: new DotEnvOptions(
                envFilePaths: new[] { envPath },
                ignoreExceptions: true));
        }
    }

    /// <summary>
    /// Loads and returns a validated <see cref="AgentSettings"/> record from 
    /// currently available environment variables.
    /// </summary>
    public static AgentSettings Load()
    {
        string? openRouterApiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
        string? googleApiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");

        if (string.IsNullOrWhiteSpace(openRouterApiKey) && string.IsNullOrWhiteSpace(googleApiKey))
        {
            throw new InvalidOperationException(
                "Configuration error: either 'OPENROUTER_API_KEY' or 'GOOGLE_API_KEY' must be present in environment variables.");
        }

        string modelName = GetRequired("MODEL_NAME");
        string temperatureRaw = GetRequired("TEMPERATURE");

        if (!double.TryParse(temperatureRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double temperature))
        {
            throw new InvalidOperationException(
                $"Configuration error: TEMPERATURE value '{temperatureRaw}' is not a valid number.");
        }

        int? maxTokens = null;
        string? maxTokensRaw = Environment.GetEnvironmentVariable("MAX_TOKENS");
        if (!string.IsNullOrWhiteSpace(maxTokensRaw))
        {
            if (!int.TryParse(maxTokensRaw, out int parsedMaxTokens))
            {
                throw new InvalidOperationException(
                    $"Configuration error: MAX_TOKENS value '{maxTokensRaw}' is not a valid integer.");
            }

            maxTokens = parsedMaxTokens;
        }

        return new AgentSettings
        {
            OpenRouterApiKey = openRouterApiKey,
            GoogleApiKey = googleApiKey,
            ModelName = modelName,
            Temperature = temperature,
            MaxTokens = maxTokens,
            TraceOutputDir = Environment.GetEnvironmentVariable("TRACE_OUTPUT_DIR"),
            ConversationOutputDir = Environment.GetEnvironmentVariable("CONVERSATION_OUTPUT_DIR"),
            DataDirectory = Environment.GetEnvironmentVariable("DATA_DIRECTORY"),
        };
    }

    /// <summary>
    /// Reads a required environment variable and throws a descriptive exception if absent.
    /// </summary>
    /// <param name="key">The name of the environment variable.</param>
    /// <returns>The value of the environment variable.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the variable is missing or empty.</exception>
    private static string GetRequired(string key)
    {
        string? value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Configuration error: required environment variable '{key}' is missing or empty. " +
                $"Ensure it is set in your .env file.");
        }

        return value;
    }
}
