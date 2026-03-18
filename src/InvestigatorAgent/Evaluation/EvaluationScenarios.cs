namespace InvestigatorAgent.Evaluation;

public static class EvaluationScenarios
{
    public static List<EvaluationScenario> GetScenarios()
    {
        return new List<EvaluationScenario>
        {
            // Happy Path Scenarios
            new() {
                Name = "Ready feature — Maintenance Scheduling",
                Category = "Happy Path",
                UserQuery = "Is the maintenance scheduling and alert system (feature1) ready for release?",
                ExpectedDecision = "READY",
                ExpectedFeatureId = "feature1"
            },
            new() {
                Name = "Ready feature — QR Code Check-in",
                Category = "Happy Path",
                UserQuery = "Can we release the QR Code Check-in system (feature2) to production?",
                ExpectedDecision = "READY",
                ExpectedFeatureId = "feature2"
            },
            new() {
                Name = "Not Ready — Contribution Tracking",
                Category = "Happy Path",
                UserQuery = "Is the Contribution Tracking (feature4) ready for deployment?",
                ExpectedDecision = "NOT READY",
                ExpectedFeatureId = "feature4"
            },

            // Ambiguous Queries
            new() {
                Name = "Partial Name — Maintenance",
                Category = "Ambiguous",
                UserQuery = "Give me a status update on the Maintenance feature.",
                ExpectedDecision = "READY",
                ExpectedFeatureId = "feature1"
            },
            new() {
                Name = "Masked Failures — Advanced Reservation",
                Category = "Ambiguous",
                UserQuery = "Is the Advanced Resource Reservation system (feature3) truly ready? Check the pipeline details carefully.",
                ExpectedDecision = "NOT READY",
                ExpectedFeatureId = "feature3"
            },
            new() {
                Name = "Multiple Matches — resource",
                Category = "Ambiguous",
                UserQuery = "Tell me about the resource features.",
                ExpectedDecision = "READY", // It should at least find one or ask for clarification, but we expect it to identify feature1/feature3
                ExpectedFeatureId = "feature1"
            },

            // Edge Cases
            new() {
                Name = "Non-existent Feature",
                Category = "Edge Case",
                UserQuery = "Check the status of the 'Blockchain Integration' feature.",
                ExpectedDecision = "NOT FOUND",
                ExpectedFeatureId = string.Empty
            },
            new() {
                Name = "Malformed ID",
                Category = "Edge Case",
                UserQuery = "Is feature-999 ready?",
                ExpectedDecision = "NOT FOUND",
                ExpectedFeatureId = string.Empty
            },

            // Tool Usage
            new() {
                Name = "Deep Dive Analysis",
                Category = "Tool Usage",
                UserQuery = "Perform a deep dive risk analysis on feature1. Look at the code and test coverage.",
                ExpectedDecision = "READY",
                ExpectedFeatureId = "feature1"
            },
            new() {
                Name = "Tool Param Check",
                Category = "Tool Usage",
                UserQuery = "What is the release summary for the QR Code feature?",
                ExpectedDecision = "READY",
                ExpectedFeatureId = "feature2"
            }
        };
    }
}
