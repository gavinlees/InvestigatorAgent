namespace InvestigatorAgent.Evaluation;

public sealed record EvaluationSummary
{
    public double OverallScore { get; init; }
    public double PassRate { get; init; }
    public int TotalScenarios { get; init; }
    public bool AcceptanceCriteriaMet { get; init; }
}

public sealed record EvaluationReport
{
    public EvaluationSummary Summary { get; init; } = new();
    public Dictionary<string, double> Dimensions { get; init; } = new();
    public List<ScenarioResult> Scenarios { get; init; } = new();
}

public sealed record ScenarioResult
{
    public string Name { get; init; } = string.Empty;
    public bool Passed { get; init; }
    public Dictionary<string, double> Scores { get; init; } = new();
    public string? Comment { get; init; }
}
