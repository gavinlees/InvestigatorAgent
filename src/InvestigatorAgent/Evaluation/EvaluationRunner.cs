using System.Diagnostics;
using System.Text.Json;
using InvestigatorAgent.Agent;
using InvestigatorAgent.Configuration;

namespace InvestigatorAgent.Evaluation;

/// <summary>
/// Executes evaluation scenarios against the Investigator Agent and logs results to Langfuse.
/// </summary>
public sealed class EvaluationRunner
{
    private readonly AgentOrchestrator _orchestrator;
    private readonly LangfuseClient _langfuseClient;
    private readonly AgentSettings _settings;

    public EvaluationRunner(AgentOrchestrator orchestrator, AgentSettings settings)
    {
        _orchestrator = orchestrator;
        _settings = settings;
        _langfuseClient = new LangfuseClient(
            settings.LangfuseBaseUrl ?? "http://localhost:3000",
            settings.LangfusePublicKey ?? string.Empty,
            settings.LangfuseSecretKey ?? string.Empty);
    }

    /// <summary>
    /// Executes the evaluation suite and logs results to Langfuse and a local JSON file.
    /// </summary>
    /// <param name="datasetName">The name of the Langfuse dataset to use.</param>
    /// <param name="createBaseline">If true, saves a copy of the results as a baseline.</param>
    public async Task RunEvaluationAsync(string datasetName = "InvestigatorAgentEvaluation", bool createBaseline = false)
    {
        Console.WriteLine($"\n🚀 Starting Evaluation: {datasetName}");
        if (createBaseline) Console.WriteLine("📝 Mode: Create Baseline");
        Console.WriteLine("--------------------------------------------------");

        await _langfuseClient.CreateOrUpdateDatasetAsync(datasetName, "Evaluation scenarios for the Investigator Agent.");

        var scenarios = EvaluationScenarios.GetScenarios();
        var results = new List<ScenarioResult>();
        string runName = $"Evaluation_{DateTime.UtcNow:yyyyMMdd_HHmm}";

        foreach (var scenario in scenarios)
        {
            Console.Write($"Running scenario [{scenario.Category}] {scenario.Name}... ");

            // Ensure dataset item exists and get its ID
            var datasetItemId = await _langfuseClient.AddDatasetItemAsync(datasetName, scenario.UserQuery, scenario.ExpectedDecision, scenario);

            string? traceId = null;
            string agentOutput = string.Empty;

            // Start a root activity to capture the trace ID accurately
            using (var activity = new ActivitySource("InvestigatorAgent.Evaluation").StartActivity("EvaluationScenario"))
            {
                traceId = Activity.Current?.TraceId.ToString();
                
                // Execute the agent
                agentOutput = await _orchestrator.SendMessageAsync(scenario.UserQuery);
            }

            // Simple Heuristic Scoring
            var scores = EvaluateResponse(scenario, agentOutput);
            bool passed = scores.Values.All(s => s >= 0.7);

            var result = new ScenarioResult
            {
                Name = scenario.Name,
                Passed = passed,
                Scores = scores,
                Comment = agentOutput.Length > 100 ? agentOutput[..100] + "..." : agentOutput
            };
            results.Add(result);

            // Log to Langfuse
            if (!string.IsNullOrEmpty(traceId) && !string.IsNullOrEmpty(datasetItemId))
            {
                // Link the trace to the dataset run
                await _langfuseClient.LogDatasetRunItemAsync(runName, datasetItemId, traceId, agentOutput);

                foreach (var score in scores)
                {
                    await _langfuseClient.PostScoreAsync(traceId, score.Key, score.Value, $"Auto-eval: {scenario.Name}");
                }
            }

            Console.WriteLine(passed ? "✅ PASS" : "❌ FAIL");
            if (!passed)
            {
                Console.WriteLine($"   > Agent Output: {agentOutput}");
                Console.WriteLine($"   > Expected Decision: {scenario.ExpectedDecision}");
                Console.WriteLine($"   > Expected Feature: {scenario.ExpectedFeatureId}");
            }
        }

        PrintSummary(results);
        await SaveResultsAsync(results, createBaseline);
    }

    /// <summary>
    /// Evaluates the agent output using deterministic heuristics.
    /// </summary>
    private Dictionary<string, double> EvaluateResponse(EvaluationScenario scenario, string output)
    {
        var scores = new Dictionary<string, double>();

        // 1. Decision Quality (Flexible matching)
        double decisionScore = 0.0;
        var decisionMapping = new Dictionary<string, string[]>
        {
            { "READY", new[] { "ready", "complete", "pass" } },
            { "NOT READY", new[] { "not ready", "cannot be released", "fail", "incomplete" } },
            { "NOT FOUND", new[] { "not found", "could not find", "cannot find", "missing", "doesn't exist", "does not exist", "don't see", "no feature", "no records" } },
            { "CLARIFICATION", new[] { "clarify", "multiple", "which one", "unsure", "confirm", "would you like", "?" } }
        };

        if (decisionMapping.TryGetValue(scenario.ExpectedDecision.ToUpperInvariant(), out var aliases))
        {
            if (aliases.Any(a => output.Contains(a, StringComparison.OrdinalIgnoreCase)))
            {
                decisionScore = 1.0;
            }
        }
        else if (output.Contains(scenario.ExpectedDecision, StringComparison.OrdinalIgnoreCase))
        {
            decisionScore = 1.0;
        }

        scores["decision_quality"] = decisionScore;

        // 2. Feature Identification
        if (!string.IsNullOrEmpty(scenario.ExpectedFeatureId))
        {
            scores["feature_id_accuracy"] = output.Contains(scenario.ExpectedFeatureId, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
        }
        else
        {
            scores["feature_id_accuracy"] = 1.0;
        }

        // 3. Completeness (heuristic)
        scores["completeness"] = output.Length > 30 ? 1.0 : 0.5;

        return scores;
    }

    /// <summary>
    /// Prints a summary of the results to the standard output.
    /// </summary>
    private void PrintSummary(List<ScenarioResult> results)
    {
        int passedCount = results.Count(r => r.Passed);
        double passRate = (double)passedCount / results.Count;

        Console.WriteLine("\n--------------------------------------------------");
        Console.WriteLine("📊 EVALUATION SUMMARY");
        Console.WriteLine($"Total Scenarios: {results.Count}");
        Console.WriteLine($"Passed:         {passedCount}");
        Console.WriteLine($"Pass Rate:      {passRate:P0}");
        Console.WriteLine("--------------------------------------------------");

        if (passRate < 0.7)
        {
            Console.WriteLine("⚠️ Warning: Acceptance criteria (70% pass rate) not met.");
        }
        else
        {
            Console.WriteLine("✨ Success: Acceptance criteria met.");
        }
    }

    /// <summary>
    /// Saves the evaluation results to a local JSON report and optionally an evaluation baseline.
    /// </summary>
    private async Task SaveResultsAsync(List<ScenarioResult> results, bool createBaseline)
    {
        var passRate = (double)results.Count(r => r.Passed) / results.Count;
        
        var report = new EvaluationReport
        {
            Summary = new EvaluationSummary
            {
                OverallScore = results.Average(r => r.Scores.Values.Average()),
                PassRate = passRate,
                TotalScenarios = results.Count,
                AcceptanceCriteriaMet = passRate >= 0.7
            },
            Dimensions = new Dictionary<string, double>
            {
                { "feature_identification", results.Average(r => r.Scores.GetValueOrDefault("feature_id_accuracy", 0)) },
                { "decision_quality", results.Average(r => r.Scores.GetValueOrDefault("decision_quality", 0)) }
            },
            Scenarios = results
        };

        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        var json = JsonSerializer.Serialize(report, options);
        
        await File.WriteAllTextAsync("evaluation_results.json", json);
        Console.WriteLine("💾 Results saved to evaluation_results.json");

        if (createBaseline)
        {
            await File.WriteAllTextAsync("evaluation_baseline.json", json);
            Console.WriteLine("🏆 Baseline created: evaluation_baseline.json");
        }
    }
}
