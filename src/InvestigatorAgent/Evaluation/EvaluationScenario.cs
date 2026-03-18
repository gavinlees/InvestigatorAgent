namespace InvestigatorAgent.Evaluation;

/// <summary>
/// Represents a single evaluation scenario for the Investigator Agent.
/// </summary>
public sealed record EvaluationScenario
{
    public string Name { get; init; } = string.Empty;
    public string UserQuery { get; init; } = string.Empty;
    public string ExpectedDecision { get; init; } = string.Empty; // e.g., "READY", "NOT READY", "CLARIFY"
    public string? ExpectedFeatureId { get; init; }
    public List<string> ExpectedTools { get; init; } = new();
    public bool ShouldCiteFailures { get; init; }
    public string Category { get; init; } = string.Empty; // Happy Path, Ambiguous, Edge Case, Tool Usage
    public string Difficulty { get; init; } = string.Empty; // Easy, Medium, Hard
}
