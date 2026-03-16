# Investigator Agent

The Investigator Agent is a .NET 10 based LLM agent built using Microsoft Semantic Kernel. 
It helps assess software feature readiness by investigating changes, assessing risks, and producing actionable reports.

## Prerequisites
- .NET 10 SDK
- OpenRouter API Key

## Setup
1. Copy `.env.example` to `.env` and fill in your API key.
2. Ensure the `incoming_data/` directory contains the necessary feature data (Jira, GitHub, Metrics). This directory is ignored by Git but required for the agent to function.
3. Run `dotnet restore`.
4. Run the application: `dotnet run --project src/InvestigatorAgent`.
