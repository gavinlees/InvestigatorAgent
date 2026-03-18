namespace InvestigatorAgent.Resilience;

/// <summary>
/// Configuration for retry policies.
/// </summary>
public record RetryConfiguration
{
    /// <summary>
    /// The maximum number of retry attempts.
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;
}
