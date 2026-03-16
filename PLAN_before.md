# Investigator Agent Implementation Plan (Semantic Kernel + C#)

## Overview

C# .NET 10 + Microsoft Semantic Kernel implementation of the Feature Readiness Investigator Agent. This document covers **how** to build the agent in C# — specific packages, project structure, testing approach, and implementation details.


## Technical Stack

**.NET 10** with async/await

The latest stable versions of each of these packages:

- **Microsoft Semantic Kernel** (`Microsoft.SemanticKernel`) for agent orchestration, tool/function calling, and LLM integration
- **OpenRouter** as the LLM provider (via Semantic Kernel's OpenAI-compatible connector with custom endpoint URI)
- **Langfuse** for evaluation, observability, and experiment tracking (via OpenTelemetry OTLP endpoint ingestion)
- **OpenTelemetry** (`OpenTelemetry`, `OpenTelemetry.Api`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`) for tracing — Semantic Kernel has built-in OTEL support
- **System.Text.Json** for data validation and serialisation
- **xUnit** for testing
- **FluentAssertions** for readable test assertions
- **NSubstitute** for mocking
- **Microsoft.Extensions.Configuration** for configuration management
- **Polly** for retry with exponential back-off
- **dotenv.net** for .env file support

## Implementation Goals
- Clear, readable C# code that shows exactly what's happening
- OpenRouter as primary LLM provider (via Semantic Kernel's OpenAI-compatible connector)
- Semantic Kernel for agent orchestration using `[KernelFunction]` attribute-based tool/function calling with `ToolCallBehavior.AutoInvokeKernelFunctions`
- Langfuse for evaluation, observability, and experiment tracking (traces sent via OTEL OTLP endpoint)
- OpenTelemetry for native tracing — Semantic Kernel emits OTEL spans automatically
- Retry mechanism with exponential back-off using Polly
- Conversation history management via Semantic Kernel's `ChatHistory` class
- Evaluation via Langfuse datasets, experiments, and REST API

## Implementation Constitution
- Clear, readable C# code that shows exactly what's happening
- For interfaces, use C# `interface` keyword — DO NOT use abstract base classes unless necessary
- NEVER put more than 1 class, interface, record, or struct in a single file
- Place unit tests in a separate test project (`InvestigatorAgent.Tests`)
- Unit test files mirror the source file structure
- The `/Tests/Integration` folder should only contain integration tests and common test assets
- Use a `.env` file (and the related `.env.example`) to manage configuration
- Use `dotnet test` to run all tests
- Use `dotnet add package` when adding dependencies
- Use EN-NZ (New Zealand English) spelling throughout

## Expectations of Quality (IMPORTANT)
In terms of priorities, code quality is the most important. How does this translate into this implementation?

- We will never accept failing tests, skipped tests, deprecation warnings, or any linting errors/warnings. These should never be skipped or suppressed without the explicit agreement with the human user.
- The code has proper decoupling to ensure ease in future features, maintenance, and fault-finding.
- Code should not be duplicated except through explicit instruction with the human user. When you think you have identified a rare scenario where it makes sense to not adhere to DRY principles, ask the opinion of the human user for guidance about how to proceed.
- All public methods MUST have XML comment blocks.
- ALWAYS use `string.Empty` instead of `""` for empty strings.
- ALWAYS use `{}` for code blocks in C#.

## Implementation Steps
The recommended order of implementation is defined in [STEPS.md](STEPS.md). The phases of implementation you will define later in this document align with this progression of steps.

<instructions_for_ai_assistant>
Read @DESIGN.md and @STEPS.md. Complete the rest of this document with implementation steps that align to these design principles and order of operations. The design allows for flexibility in certain areas. When you have multiple options, ask the user what their preference is — do not make assumptions or fundamental design decisions on your own.

**Ensure intuitive validation**: The developers completing these steps are strongly encouraged to validate the acceptance criteria for each step via automated tests and manual verification BEFORE moving on to the next step. The end of each step should be verifiable with automated and manual tests.

After ensuring you have all of the user preferences needed to proceed, create a detailed implementation plan below.
</instructions_for_ai_assistant>
