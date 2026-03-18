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

    public async Task RunEvaluationAsync(string datasetName = "InvestigatorAgentEvaluation")
    {
        Console.WriteLine($"\n🚀 Starting Evaluation: {datasetName}");
        Console.WriteLine("--------------------------------------------------");

        await _langfuseClient.CreateOrUpdateDatasetAsync(datasetName, "Evaluation scenarios for the Investigator Agent.");

        var scenarios = EvaluationScenarios.GetScenarios();
        var results = new List<ScenarioResult>();

        foreach (var scenario in scenarios)
        {
            Console.Write($"Running scenario [{scenario.Category}] {scenario.Name}... ");

            // Ensure dataset item exists (simplified: always add/update)
            // In a real app, we might fetch item IDs first.
            await _langfuseClient.AddDatasetItemAsync(datasetName, scenario.UserQuery, scenario.ExpectedDecision, scenario);

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
            if (!string.IsNullOrEmpty(traceId))
            {
                // Note: We need the datasetItemId to link correctly. 
                // For simplicity in this module, we'll post scores to the trace directly.
                foreach (var score in scores)
                {
                    await _langfuseClient.PostScoreAsync(traceId, score.Key, score.Value, $"Auto-eval: {scenario.Name}");
                }
            }

            Console.WriteLine(passed ? "✅ PASS" : "❌ FAIL");
        }

        PrintSummary(results);
    }

    private Dictionary<string, double> EvaluateResponse(EvaluationScenario scenario, string output)
    {
        var scores = new Dictionary<string, double>();

        // 1. Decision Quality
        double decisionScore = output.Contains(scenario.ExpectedDecision, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
        scores["decision_quality"] = decisionScore;

        // 2. Feature Identification
        if (!string.IsNullOrEmpty(scenario.ExpectedFeatureId))
        {
            scores["feature_id_accuracy"] = output.Contains(scenario.ExpectedFeatureId, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
        }

        // 3. Completeness (heuristic)
        scores["completeness"] = output.Length > 50 ? 1.0 : 0.5;

        return scores;
    }

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

        if (passRate < 0.8)
        {
            Console.WriteLine("⚠️ Warning: Acceptance criteria (80% pass rate) not met.");
        }
        else
        {
            Console.WriteLine("✨ Success: Acceptance criteria met.");
        }
    }
}
