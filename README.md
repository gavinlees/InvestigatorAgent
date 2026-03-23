# Investigator Agent

The Investigator Agent is a specialized tool designed to assist in the evaluation of software features for release readiness. It leverages the Microsoft Semantic Kernel and OpenRouter to investigate feature status across multiple data sources (Jira, GitHub, and System Metrics).

## Prerequisites

- .NET 10 SDK
- OpenRouter API Key

## Setup

1. Copy `.env.example` to `.env` and fill in your API key.
2. Ensure the `incoming_data/` directory contains the necessary feature data (Jira, GitHub, Metrics). This directory is ignored by Git but required for the agent to function.
3. Run `dotnet restore`.
4. Run the application: `dotnet run --project src/InvestigatorAgent`.

## Evaluation & Metrics

The Investigator Agent includes a comprehensive evaluation suite to measure decision accuracy and tool usage quality.

### Running Evaluations

To run the evaluation suite against the defined scenarios:

```bash
dotnet run --project src/InvestigatorAgent -- --eval
```

### Creating a Baseline

To save the current results as a baseline for future comparison:

```bash
dotnet run --project src/InvestigatorAgent -- --eval --create-baseline
```

Results are saved locally to `evaluation_results.json` and synchronized with the Langfuse dashboard for deeper analysis.
