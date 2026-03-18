using Polly;
using Polly.Retry;
using Microsoft.Extensions.Logging;

namespace InvestigatorAgent.Resilience;

/// <summary>
/// Provides factory methods for creating Polly retry policies.
/// </summary>
public static class RetryPolicies
{
    /// <summary>
    /// Creates a retry policy for LLM API calls.
    /// Retries on transient errors with exponential back-off and jitter.
    /// </summary>
    public static AsyncRetryPolicy CreateLlmRetryPolicy(RetryConfiguration config, ILogger? logger = null)
    {
        return Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: config.MaxRetryAttempts,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                    + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    logger?.LogWarning(
                        exception,
                        "LLM API call failed. Initialising retry {RetryCount} of {MaxRetries} after {Delay}ms.",
                        retryCount,
                        config.MaxRetryAttempts,
                        timeSpan.TotalMilliseconds);
                });
    }

    /// <summary>
    /// Creates a retry policy for tool file I/O operations.
    /// </summary>
    public static AsyncRetryPolicy CreateToolRetryPolicy(RetryConfiguration config, ILogger? logger = null)
    {
        return Policy
            .Handle<FileNotFoundException>()
            .Or<IOException>()
            .WaitAndRetryAsync(
                retryCount: config.MaxRetryAttempts,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                    + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    logger?.LogWarning(
                        exception,
                        "File tool operation failed. Initialising retry {RetryCount} of {MaxRetries} after {Delay}ms.",
                        retryCount,
                        config.MaxRetryAttempts,
                        timeSpan.TotalMilliseconds);
                });
    }
}
