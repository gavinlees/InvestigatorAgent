# Investigator Agent Implementation Plan (Semantic Kernel + C#)

## Overview

C# .NET 10 + Microsoft Semantic Kernel implementation of the Feature Readiness Investigator Agent. This document covers **how** to build the agent in C# — specific packages, project structure, testing approach, and implementation details.


## Technical Stack

**.NET 10** with async/await

The latest stable versions of each of these packages:

- **Microsoft Semantic Kernel** (`Microsoft.SemanticKernel`) for agent orchestration, tool/function calling, and LLM integration
- **OpenRouter** as the LLM provider (via Semantic Kernel's OpenAI-compatible connector with custom endpoint URI)
- **Langfuse** for evaluation, observability, and experiment tracking (via OpenTelemetry OTLP endpoint ingestion + REST API)
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


## Project Structure

```
investigator-agent/
├── InvestigatorAgent.sln
├── src/
│   └── InvestigatorAgent/
│       ├── InvestigatorAgent.csproj
│       ├── Program.cs                          # CLI entry point
│       ├── Plugins/
│       │   ├── JiraPlugin.cs                   # [KernelFunction] get_jira_data
│       │   └── AnalysisPlugin.cs               # [KernelFunction] get_analysis
│       ├── Agent/
│       │   ├── AgentOrchestrator.cs            # Semantic Kernel chat loop
│       │   └── SystemPrompts.cs                # System prompts and templates
│       ├── Observability/
│       │   └── TelemetrySetup.cs               # OpenTelemetry → Langfuse configuration
│       ├── Configuration/
│       │   ├── AgentSettings.cs                # Strongly-typed configuration
│       │   └── ConfigurationLoader.cs          # .env file loading
│       ├── Persistence/
│       │   ├── IConversationStore.cs           # Interface for conversation persistence
│       │   └── FileConversationStore.cs        # JSON file conversation persistence
│       ├── Resilience/
│       │   ├── RetryPolicies.cs                # Polly retry policy definitions
│       │   └── RetryConfiguration.cs           # Retry configuration settings
│       └── Utils/
│           ├── FeatureFolderMapper.cs           # Feature ID → folder mapping
│           └── FileUtils.cs                     # File reading utilities
├── tests/
│   └── InvestigatorAgent.Tests/
│       ├── InvestigatorAgent.Tests.csproj
│       ├── Plugins/
│       │   ├── JiraPluginTests.cs
│       │   └── AnalysisPluginTests.cs
│       ├── Agent/
│       │   ├── AgentOrchestratorTests.cs
│       │   └── SystemPromptsTests.cs
│       ├── Configuration/
│       │   └── AgentSettingsTests.cs
│       ├── Persistence/
│       │   └── FileConversationStoreTests.cs
│       ├── Resilience/
│       │   └── RetryPoliciesTests.cs
│       ├── Utils/
│       │   ├── FeatureFolderMapperTests.cs
│       │   └── FileUtilsTests.cs
│       ├── Integration/
│       │   ├── AgentEndToEndTests.cs
│       │   └── PluginsIntegrationTests.cs
│       ├── Evaluation/
│       │   ├── EvaluationRunner.cs             # Langfuse REST API integration
│       │   ├── EvaluationScenarios.cs
│       │   └── EvaluationRunnerTests.cs
│       └── Fixtures/
│           └── SampleDataFixture.cs
├── traces/                                      # Local trace backup (JSON)
├── conversations/                               # Persisted conversation JSON files
├── incoming_data/                               # Test data (provided)
│   ├── feature1/
│   ├── feature2/
│   ├── feature3/
│   └── feature4/
├── .env                                         # Configuration (gitignored)
├── .env.example                                 # Example configuration template
├── .gitignore
└── README.md
```

### Key Architectural Decisions

**Semantic Kernel Plugins vs Custom Tools:**
Semantic Kernel uses a "Plugin" model where tools are C# classes with methods decorated with `[KernelFunction]` and `[Description]`. The kernel automatically handles:
- Function schema generation from parameter types and descriptions
- Tool call routing and parameter binding
- Auto-invocation when `ToolCallBehavior.AutoInvokeKernelFunctions` is set

This replaces the manual `ITool` interface pattern used in the Module 6 DetectiveAgent.

**Langfuse Integration Architecture:**
```
Semantic Kernel (C#)
    → Built-in OTEL ActivitySource ("Microsoft.SemanticKernel*")
    → OpenTelemetry SDK TracerProvider
    → OTLP Exporter → Langfuse /api/public/otel endpoint
    → Langfuse UI (traces, evaluations, datasets, experiments)

Evaluation Runner (C#)
    → HttpClient → Langfuse REST API
    → Create datasets, log scores, run experiments
```

## Implementation Guide by Step

This guide follows the order defined in [STEPS.md](STEPS.md) and builds incrementally with verification at each step.

---

### **Step 1: UI Setup, Agent Configuration & Conversations**

**Goal:** Set up a CLI to interact with the agent and configure basic agent behaviour

**Tasks:**

#### 1.1 Project Initialisation
- Create solution: `dotnet new sln -n InvestigatorAgent`
- Create console project: `dotnet new console -n InvestigatorAgent -o src/InvestigatorAgent`
- Create test project: `dotnet new xunit -n InvestigatorAgent.Tests -o tests/InvestigatorAgent.Tests`
- Add projects to solution
- Create .env.example with required configuration variables
- Create .gitignore (ignore .env, bin/, obj/, traces/, conversations/, .vs/, *.user)
- Create basic README.md
- Initialise git repo, add remote `gavinlnz`

**Dependencies to add:**
```bash
# Main project
dotnet add src/InvestigatorAgent package Microsoft.SemanticKernel
dotnet add src/InvestigatorAgent package dotenv.net
dotnet add src/InvestigatorAgent package Microsoft.Extensions.Configuration
dotnet add src/InvestigatorAgent package Microsoft.Extensions.Configuration.EnvironmentVariables

# Test project
dotnet add tests/InvestigatorAgent.Tests package FluentAssertions
dotnet add tests/InvestigatorAgent.Tests package NSubstitute
dotnet add tests/InvestigatorAgent.Tests reference src/InvestigatorAgent
```

**Acceptance Criteria:**
- ✅ Solution builds without errors or warnings
- ✅ All dependencies install without errors
- ✅ .env.example documents all required config variables
- ✅ Project structure matches the layout above
- ✅ Git repository initialised with proper .gitignore

---

#### 1.2 Configuration Management
**Files:** `Configuration/AgentSettings.cs`, `Configuration/ConfigurationLoader.cs`, `AgentSettingsTests.cs`, `.env.example`

**Implementation:**
- Create `AgentSettings` record for strongly-typed configuration
- Load from .env file using `dotenv.net`
- Validate required fields (API key, model name, temperature)
- Support optional fields (max tokens, trace output directory)

**Configuration Variables (.env.example):**
```bash
OPENROUTER_API_KEY=your_api_key_here
MODEL_NAME=openai/gpt-4o-mini
TEMPERATURE=0.0
MAX_TOKENS=4096
TRACE_OUTPUT_DIR=traces/
CONVERSATION_OUTPUT_DIR=conversations/
DATA_DIRECTORY=incoming_data/

# Langfuse (for Steps 4 & 6)
LANGFUSE_PUBLIC_KEY=pk-lf-your_key_here
LANGFUSE_SECRET_KEY=sk-lf-your_key_here
LANGFUSE_BASE_URL=http://localhost:3000
```

**Acceptance Criteria:**
- ✅ Config loads from .env file
- ✅ Missing required fields raise clear validation errors
- ✅ Unit tests verify config loading and validation
- ✅ Config is type-safe via `AgentSettings` record

---

#### 1.3 Basic Agent Core (No Tools Yet)
**Files:** `Agent/AgentOrchestrator.cs`, `Agent/SystemPrompts.cs`, tests

**Implementation:**
- Create system prompt defining agent role and purpose
- Initialise Semantic Kernel with OpenRouter configuration
- Build basic chat loop: receive message → call LLM → return response
- Use Semantic Kernel's `ChatHistory` for conversation state management

**Semantic Kernel Setup with OpenRouter:**
```csharp
#pragma warning disable SKEXP0010
var builder = Kernel.CreateBuilder();
builder.AddOpenAIChatCompletion(
    modelId: settings.ModelName,
    apiKey: settings.OpenRouterApiKey,
    endpoint: new Uri("https://openrouter.ai/api/v1")
);
#pragma warning restore SKEXP0010
```

**System Prompt (Initial):**
```
You are the Investigator Agent for the CommunityShare platform.

Your role is to assess whether software features are ready to progress through
the development pipeline (Development → UAT → Production).

You help product managers and engineering teams make informed decisions about
feature readiness by analysing:
- Feature metadata (JIRA tickets, status, context)
- Test metrics (unit tests, coverage, failures)
- Risk factors and blockers

When asked about a feature's readiness, you:
1. Identify which feature the user is asking about
2. Gather relevant data using available tools
3. Analyse the data against readiness criteria
4. Provide clear recommendations with reasoning

Be concise, helpful, and transparent about your analysis process.
```

**Agent Loop:**
```csharp
// Agent/AgentOrchestrator.cs
public class AgentOrchestrator
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatService;
    private readonly ChatHistory _chatHistory;

    public async Task<string> SendMessageAsync(string userMessage)
    {
        _chatHistory.AddUserMessage(userMessage);
        var result = await _chatService.GetChatMessageContentsAsync(
            _chatHistory,
            kernel: _kernel
        );
        var response = result[0].Content ?? string.Empty;
        _chatHistory.AddAssistantMessage(response);
        return response;
    }
}
```

**Acceptance Criteria:**
- ✅ Semantic Kernel initialises with OpenRouter without errors
- ✅ Agent responds to basic questions about its purpose
- ✅ Conversation loop works (send message, get response)
- ✅ ChatHistory maintains state across turns
- ✅ Unit tests verify orchestration logic

---

#### 1.4 CLI Interface
**Files:** `Program.cs`

**Implementation:**
- Create simple REPL interface
- Load configuration from .env
- Initialise Semantic Kernel agent
- Accept user input and display agent responses
- Support exit command

**Usage:**
```bash
dotnet run --project src/InvestigatorAgent

Investigator Agent CLI
Type 'exit' to quit

You: What do you do?
Agent: I help assess whether software features are ready to progress...

You: exit
Goodbye!
```

**Acceptance Criteria:**
- ✅ CLI starts without errors
- ✅ User can input questions and receive responses
- ✅ Exit command works cleanly
- ✅ Configuration errors show helpful messages

---

#### 1.5 Step 1 Validation

**Manual Testing:**
```bash
# Test 1: Agent explains its purpose
You: What do you do?
Expected: Agent explains feature readiness assessment role

# Test 2: Agent asks for clarification on vague queries
You: Is the payment feature ready?
Expected: Agent asks for more details (no tools yet, so can't look anything up)
```

**Automated Testing:**
```bash
dotnet test
# Expected: All tests pass, no warnings
```

**Success Criteria:**
- ✅ CLI runs and accepts input
- ✅ Agent responds conversationally
- ✅ Agent personality matches system prompt
- ✅ All unit tests pass with no deprecation warnings

---

### **Step 2: Feature Lookup Tool & Error Handling/Persistence**

**Goal:** Agent can look up feature metadata using JIRA plugin

**Tasks:**

#### 2.1 Understand Test Data Structure
**Action:** Explore `incoming_data/` to understand JIRA file structure

**Key Observations:**
- 4 features: feature1/, feature2/, feature3/, feature4/
- Each has `jira/feature_issue.json` with metadata
- Need to map feature names → folder names → JIRA data

**Create mapping utility:**
```csharp
// Utils/FeatureFolderMapper.cs
public class FeatureFolderMapper
{
    /// <summary>
    /// Maps feature IDs to their folder paths by scanning the data directory.
    /// </summary>
    public Dictionary<string, string> GetFeatureFolderMapping(string dataDirectory) { ... }
}
```

---

#### 2.2 Implement JIRA Plugin
**Files:** `Plugins/JiraPlugin.cs`, `JiraPluginTests.cs`

**Implementation using Semantic Kernel's `[KernelFunction]` pattern:**
```csharp
// Plugins/JiraPlugin.cs
public class JiraPlugin
{
    private readonly string _dataDirectory;

    public JiraPlugin(string dataDirectory)
    {
        _dataDirectory = dataDirectory;
    }

    /// <summary>
    /// Retrieves metadata for ALL features from JIRA.
    /// Returns folder, jira_key, feature_id, summary, status, and data_quality
    /// for each feature.
    /// </summary>
    [KernelFunction("get_jira_data")]
    [Description("Retrieves metadata for ALL features from JIRA. Returns feature_id, jira_key, summary, status for each feature. Use this first when a user asks about a feature.")]
    public async Task<string> GetJiraDataAsync()
    {
        // Read from incoming_data/feature*/jira/feature_issue.json
        // Parse and return JSON list of features
    }
}
```

**Register Plugin with Semantic Kernel:**
```csharp
var jiraPlugin = new JiraPlugin(settings.DataDirectory);
kernel.Plugins.AddFromObject(jiraPlugin, "JiraPlugin");
```

**Error Handling:**
- Missing JIRA files → return graceful error message in function output
- Malformed JSON → log error, return partial data if possible
- Empty directory → return empty list with note

**Acceptance Criteria:**
- ✅ Plugin retrieves all 4 features from test data
- ✅ Returns correct schema: folder, jira_key, feature_id, summary, status
- ✅ Handles missing files gracefully
- ✅ Unit tests verify plugin behaviour with mock data
- ✅ Unit tests verify error handling

---

#### 2.3 Integrate JIRA Plugin & Enable Auto Function Calling
**Files:** `Agent/AgentOrchestrator.cs`, `Agent/SystemPrompts.cs`

**Implementation:**
- Register `JiraPlugin` with the kernel
- Enable auto function calling via execution settings
- Update system prompt with tool usage guidance

**Enable Auto Function Calling:**
```csharp
var executionSettings = new OpenAIPromptExecutionSettings
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
};

var result = await _chatService.GetChatMessageContentsAsync(
    _chatHistory,
    executionSettings: executionSettings,
    kernel: _kernel
);
```

**Updated System Prompt Addition:**
```
## Available Tools

You have access to these tools:

1. **get_jira_data()**: Retrieves metadata for ALL features
   - Use this first when user asks about a feature
   - Returns: feature_id, jira_key, summary, status for all features
   - You'll need the feature_id for subsequent analysis

## Workflow

When asked "Is [feature name] ready for its next phase?":
1. Call get_jira_data() to find all features
2. Identify which feature matches the user's query
3. Extract the feature_id for that feature
4. (More tools will be available in later steps)
```

**Acceptance Criteria:**
- ✅ Plugin successfully registered with kernel
- ✅ Semantic Kernel auto-invokes get_jira_data when appropriate
- ✅ Agent uses tool results to identify features
- ✅ Integration tests verify tool calling

---

#### 2.4 Conversation Persistence
**Files:** `Persistence/IConversationStore.cs`, `Persistence/FileConversationStore.cs`, `FileConversationStoreTests.cs`

**Implementation:**
- Create `IConversationStore` interface for persistence abstraction
- Implement `FileConversationStore` that saves conversations as JSON files
- Save after each turn (not just at session end)
- Format: `conversations/conv_yyyyMMdd_HHmmss_xxxxx.json`
- Structure includes: conversation_id, system_prompt, metadata (model, temperature), complete turn history

**Acceptance Criteria:**
- ✅ Conversations saved as JSON files after each turn
- ✅ Agent remembers previous messages in same session
- ✅ Files are human-readable and machine-parseable
- ✅ Unit tests verify persistence behaviour

---

#### 2.5 Step 2 Validation

**Manual Testing:**
```bash
You: Is the maintenance scheduling feature ready for its next phase?
Expected:
- Agent calls get_jira_data()
- Agent identifies the feature from results
- Agent mentions it found feature but needs more data (no analysis tool yet)

You: What features do you know about?
Expected:
- Agent calls get_jira_data()
- Agent lists all 4 features with summaries
```

**Automated Testing:**
```bash
dotnet test
```

**Success Criteria:**
- ✅ Agent successfully retrieves JIRA data via auto function calling
- ✅ Agent identifies correct feature from natural language query
- ✅ Agent handles ambiguous feature names (asks for clarification)
- ✅ Conversations persisted to filesystem
- ✅ All tests pass with no warnings

---

### **Step 3: Testing Metrics Results Tool**

**Goal:** Agent analyses test metrics and makes readiness decisions

**Tasks:**

#### 3.1 Understand Analysis Data Structure
**Action:** Explore `incoming_data/feature*/` to understand analysis files

**Available Analysis Types in incoming_data:**
```
metrics/
  - performance_benchmarks.json
  - pipeline_results.json
  - security_scan_results.json
  - test_coverage_report.json
  - unit_test_results.json
reviews/
  - security.json
  - stakeholders.json
  - uat.json
```

**IMPORTANT — Scope for This Module:**
For this module, we are ONLY implementing support for:
- `metrics/unit_test_results`
- `metrics/test_coverage_report`

Other analysis types will be added in future modules.

---

#### 3.2 Implement Analysis Plugin
**Files:** `Plugins/AnalysisPlugin.cs`, `AnalysisPluginTests.cs`

**Implementation using Semantic Kernel `[KernelFunction]`:**
```csharp
// Plugins/AnalysisPlugin.cs
public class AnalysisPlugin
{
    /// <summary>
    /// Retrieves analysis data for a specific feature.
    /// Supported analysis types: 'metrics/unit_test_results', 'metrics/test_coverage_report'.
    /// </summary>
    [KernelFunction("get_analysis")]
    [Description("Retrieves analysis data for a specific feature. Requires feature_id and analysis_type parameters.")]
    public async Task<string> GetAnalysisAsync(
        [Description("The feature ID (e.g., 'feature1', 'feature2')")] string featureId,
        [Description("Analysis type: 'metrics/unit_test_results' or 'metrics/test_coverage_report'")] string analysisType)
    {
        // Map feature_id → folder using FeatureFolderMapper
        // Read incoming_data/{featureId}/{analysisType}.json
        // Return parsed result
    }
}
```

**Note:** Semantic Kernel automatically generates the function schema from the parameter types and `[Description]` attributes — no manual schema definition needed. The agent will call the function with the correct parameters.

**Error Handling:**
- Invalid feature_id → return error message
- Invalid analysis_type → return list of valid types
- Missing file → return note that analysis type not available for this feature
- Malformed JSON → return error with details

**Acceptance Criteria:**
- ✅ Plugin retrieves both testing analysis types correctly
- ✅ Handles invalid inputs gracefully
- ✅ Unit tests verify error cases

---

#### 3.3 Integrate Analysis Plugin & Update Prompt
**Files:** `Agent/AgentOrchestrator.cs`, `Agent/SystemPrompts.cs`

**Register Plugin:**
```csharp
var analysisPlugin = new AnalysisPlugin(settings.DataDirectory, mapper);
kernel.Plugins.AddFromObject(analysisPlugin, "AnalysisPlugin");
```

**Updated System Prompt Addition:**
```
2. **get_analysis(feature_id, analysis_type)**: Retrieves specific analysis data
   - Requires feature_id from get_jira_data()
   - Analysis types available in this version:
     * 'metrics/unit_test_results' - Check for test failures
     * 'metrics/test_coverage_report' - Review test coverage
   - Call both analysis types to make comprehensive decisions

## Decision Criteria

When determining if a feature is ready for its next phase:

**Critical Rule: ANY failing tests = NOT READY**

For Development → UAT:
- ✅ All unit tests must pass (0 failures)
- ✅ Code review completed
- ✅ Security review shows LOW or MEDIUM risk (HIGH = blocker)

For UAT → Production:
- ✅ All unit tests must pass (0 failures)
- ✅ UAT testing completed with no critical issues
- ✅ Security review shows LOW risk only
- ✅ Stakeholder approvals obtained

**Always provide specific reasoning:**
- Cite exact test failure counts
- Reference specific blockers from analysis data
- Explain which criteria are met/not met
```

**Acceptance Criteria:**
- ✅ Agent can call get_analysis with correct parameters
- ✅ Agent calls multiple analysis types for comprehensive assessment
- ✅ Agent makes decisions based on test results
- ✅ Integration tests verify decision logic

---

#### 3.4 Step 3 Validation

**Manual Testing:**

Test Case 1: Feature with failing tests
```bash
You: Is the QR code check-in feature ready for its next phase?
Expected:
- Agent calls get_jira_data()
- Agent identifies feature
- Agent calls get_analysis for unit tests
- Agent sees failures → recommends NOT READY
- Agent provides specific failure details
```

Test Case 2: Feature with all tests passing
```bash
You: Is the maintenance scheduling feature ready for its next phase?
Expected:
- Agent calls get_jira_data()
- Agent identifies feature
- Agent calls multiple analysis types
- Agent sees all tests passing → recommends READY
```

Test Case 3: Ambiguous feature name
```bash
You: Is the reservation system ready?
Expected:
- Agent calls get_jira_data()
- Agent finds multiple possible matches or no confident match
- Agent asks user for clarification
```

**Automated Testing:**
```bash
dotnet test
```

**Success Criteria:**
- ✅ All acceptance criteria from DESIGN.md are met
- ✅ Agent correctly identifies features from natural language
- ✅ Agent makes appropriate readiness decisions based on test results
- ✅ Agent provides clear reasoning with specific evidence
- ✅ Agent handles missing/ambiguous data gracefully
- ✅ All tests pass with no warnings

---

### **Step 4: Observability Tracing**

**Goal:** Add comprehensive OpenTelemetry tracing, exported to Langfuse

**Important Context:**
Semantic Kernel has **built-in** OpenTelemetry support. It emits traces via the `Microsoft.SemanticKernel*` activity source, and Langfuse can ingest these traces via its OTLP HTTP endpoint. This means:
- No custom trace instrumentation needed for LLM calls — Semantic Kernel handles it
- We configure OTEL → OTLP exporter → Langfuse endpoint
- All LLM calls, function calls, and timings are captured automatically

**Tasks:**

#### 4.1 Langfuse Setup
- Set up Langfuse (either self-hosted via Docker or cloud free tier)
- Configure API keys in .env
- Verify Langfuse is accessible

**Docker Setup (self-hosted):**
```bash
# Clone Langfuse
git clone https://github.com/langfuse/langfuse.git
cd langfuse
docker compose up -d
# Langfuse UI available at http://localhost:3000
```

---

#### 4.2 OpenTelemetry → Langfuse Configuration
**Files:** `Observability/TelemetrySetup.cs`

**Dependencies:**
```bash
dotnet add src/InvestigatorAgent package OpenTelemetry
dotnet add src/InvestigatorAgent package OpenTelemetry.Api
dotnet add src/InvestigatorAgent package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add src/InvestigatorAgent package OpenTelemetry.Extensions.Hosting
```

**Implementation:**
```csharp
// Observability/TelemetrySetup.cs
public static class TelemetrySetup
{
    /// <summary>
    /// Configures OpenTelemetry tracing to export Semantic Kernel spans to Langfuse.
    /// </summary>
    public static TracerProvider ConfigureTracing(AgentSettings settings)
    {
        // Enable Semantic Kernel diagnostic output
        AppContext.SetSwitch(
            "Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive",
            true);

        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService("InvestigatorAgent");

        return Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddSource("Microsoft.SemanticKernel*")
            .AddOtlpExporter(options =>
            {
                // Langfuse OTLP endpoint
                options.Endpoint = new Uri($"{settings.LangfuseBaseUrl}/api/public/otel");
                options.Headers = $"Authorization=Basic {Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(
                        $"{settings.LangfusePublicKey}:{settings.LangfuseSecretKey}"))}";
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
            })
            .Build();
    }
}
```

**What Semantic Kernel Automatically Traces:**
- Chat completion requests and responses
- Function/tool call invocations and results
- Token usage (prompt + completion tokens)
- Model name, temperature, and other parameters
- Request latency

**Acceptance Criteria:**
- ✅ OpenTelemetry initialised correctly
- ✅ Traces appear in Langfuse UI
- ✅ All Semantic Kernel operations captured automatically
- ✅ Token usage and timing data visible in Langfuse

---

#### 4.3 Local Trace Backup (Optional)
**Files:** `Observability/TelemetrySetup.cs` (extend)

**Implementation:**
- Add a secondary JSON file exporter alongside the Langfuse OTLP exporter
- Traces saved to `traces/` directory as backup
- Format: `traces/trace_{timestamp}.json`

This provides local trace files even when Langfuse is unavailable.

---

#### 4.4 Step 4 Validation

**Manual Testing:**
```bash
# Run a test conversation
You: Is the maintenance scheduling feature ready?
Agent: [provides answer]

# Check Langfuse UI at http://localhost:3000
# Navigate to Traces tab
# Verify: Complete trace with LLM call and function call spans

# Also check local backup
ls traces/
# Should see trace files
```

**Automated Testing:**
```bash
dotnet test
```

**Success Criteria:**
- ✅ Every conversation generates traces in Langfuse
- ✅ Traces include all operations (LLM calls, function calls)
- ✅ Token usage and timing data captured
- ✅ Traces visible and navigable in Langfuse UI
- ✅ Can correlate conversation flow with trace spans

---

### **Step 5: Retry with Exponential Back-off**

**Goal:** Configure retry mechanisms for LLM and tool failures using Polly

**Important Context:**
The C# ecosystem uses **Polly** — the industry-standard resilience library for .NET. Polly provides:
- Exponential back-off with jitter
- Circuit breaker patterns
- Retry policies scoped to specific exception types
- Full integration with dependency injection

**Tasks:**

#### 5.1 Add Polly Dependencies
```bash
dotnet add src/InvestigatorAgent package Polly
dotnet add src/InvestigatorAgent package Polly.Extensions.Http
```

---

#### 5.2 Define Retry Policies
**Files:** `Resilience/RetryPolicies.cs`, `Resilience/RetryConfiguration.cs`

**Implementation:**
```csharp
// Resilience/RetryPolicies.cs
public static class RetryPolicies
{
    /// <summary>
    /// Creates a retry policy for LLM API calls.
    /// Retries on transient errors with exponential back-off and jitter.
    /// </summary>
    public static AsyncRetryPolicy CreateLlmRetryPolicy(RetryConfiguration config)
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
                    // Log retry attempt via ILogger
                });
    }

    /// <summary>
    /// Creates a retry policy for tool file I/O operations.
    /// </summary>
    public static AsyncRetryPolicy CreateToolRetryPolicy(RetryConfiguration config)
    {
        return Policy
            .Handle<FileNotFoundException>()
            .Or<IOException>()
            .WaitAndRetryAsync(
                retryCount: config.MaxRetryAttempts,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                    + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500)));
    }
}
```

**Acceptance Criteria:**
- ✅ LLM calls configured with retry logic
- ✅ Plugin file I/O configured with retry logic
- ✅ Retry only on transient errors (network, timeout, I/O)
- ✅ Retry configuration is externalised to settings
- ✅ Unit tests verify retry behaviour with mocked failures

---

#### 5.3 Apply Retry to Agent Operations
**Files:** `Agent/AgentOrchestrator.cs`, `Plugins/JiraPlugin.cs`, `Plugins/AnalysisPlugin.cs`

**Implementation:**
- Wrap LLM calls with Polly retry policy
- Wrap plugin file I/O with tool retry policy
- Add logging for retry attempts
- Ensure graceful degradation after retry exhaustion

**Acceptance Criteria:**
- ✅ Plugins configured with appropriate retry logic
- ✅ Graceful error messages after retry exhaustion
- ✅ Retry attempts visible in Langfuse traces
- ✅ Unit tests verify retry behaviour with mocked failures
- ✅ Permanent errors (validation, logic) don't trigger retries

---

#### 5.4 Step 5 Validation

**Automated Testing:**
```bash
dotnet test --filter "Category=Retry"
```

**Success Criteria:**
- ✅ LLM calls retry on network/API errors (3 attempts max)
- ✅ Plugins retry on transient I/O errors (3 attempts max)
- ✅ Exponential back-off with jitter applied
- ✅ Retry attempts visible in Langfuse traces
- ✅ Graceful error messages after retry exhaustion
- ✅ All tests pass

---

### **Step 6: Evaluation**

**Goal:** Automated evaluation and performance tracking via Langfuse

**Important Context:**
Langfuse provides:
- **Datasets**: Collections of input/expected-output pairs
- **Experiments**: Run your agent against a dataset and collect results
- **Scores**: Attach scores (numeric or boolean) to traces
- **REST API**: Programmatically create datasets, log experiments, and retrieve scores

We'll build a C# `EvaluationRunner` that:
1. Defines test scenarios as a Langfuse dataset (via REST API)
2. Runs the agent against each scenario
3. Evaluates responses using custom scoring logic
4. Logs scores back to Langfuse via REST API
5. Generates a summary report

**Tasks:**

#### 6.1 Define Test Scenarios
**Files:** `Evaluation/EvaluationScenarios.cs`

**Implementation:**
```csharp
public class EvaluationScenario
{
    public string Name { get; init; } = string.Empty;
    public string UserQuery { get; init; } = string.Empty;
    public string ExpectedDecision { get; init; } = string.Empty;
    public string? ExpectedFeatureId { get; init; }
    public List<string> ExpectedTools { get; init; } = new();
    public bool ShouldCiteFailures { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Difficulty { get; init; } = string.Empty;
}
```

**Scenario Categories (minimum 8 examples):**
1. **Happy Path**: Clear feature names, complete data, obvious decisions
   - Feature with all tests passing → READY decision
   - Feature with failing tests → NOT READY decision
2. **Ambiguous Queries**: Partial names, multiple possible matches
   - Unclear feature reference → clarification request
3. **Edge Cases**: Missing data, unexpected states
   - Missing test data → appropriate handling
   - Invalid feature name → helpful error message
4. **Tool Usage**: Verify correct tool calling patterns
   - Should call both get_jira_data and get_analysis
   - Should use correct feature_id parameter

---

#### 6.2 Build Langfuse Evaluation Runner
**Files:** `Evaluation/EvaluationRunner.cs`

**Implementation:**
- Create/update Langfuse dataset via REST API
- Run each scenario against the agent
- Score responses using custom evaluators:
  - **Feature Identification Accuracy**: Did agent identify the correct feature?
  - **Tool Usage Correctness**: Did agent call the right tools with right parameters?
  - **Decision Quality**: Did agent make the correct readiness decision with evidence?
- Log scores to Langfuse via REST API
- Generate JSON summary report locally

**Langfuse REST API Usage:**
```csharp
// POST /api/public/v2/datasets — Create dataset
// POST /api/public/v2/dataset-items — Add test scenarios
// POST /api/public/v2/dataset-run-items — Log experiment results
// POST /api/public/v2/scores — Attach scores to traces
```

**Report Format:**
```json
{
  "summary": {
    "overall_score": 0.87,
    "pass_rate": 0.92,
    "total_scenarios": 10,
    "acceptance_criteria_met": true
  },
  "dimensions": {
    "feature_identification": 0.95,
    "tool_usage_correctness": 0.91,
    "decision_quality": 0.85
  },
  "scenarios": [
    {
      "name": "Ready feature — maintenance scheduling",
      "passed": true,
      "scores": { ... }
    }
  ]
}
```

---

#### 6.3 CLI Integration
**Files:** `Program.cs` (add eval mode)

**Implementation:**
- Add `--eval` flag to run evaluation mode
- Add `--create-baseline` flag to save baseline results
- Print summary to console with pass/fail indicators

**Usage:**
```bash
# Run evaluation
dotnet run --project src/InvestigatorAgent -- --eval

# Create baseline
dotnet run --project src/InvestigatorAgent -- --eval --create-baseline

# Normal conversation mode
dotnet run --project src/InvestigatorAgent
```

---

#### 6.4 Step 6 Validation

**Manual Testing:**
```bash
# Test 1: Run evaluation
dotnet run --project src/InvestigatorAgent -- --eval
# Verify: Evaluation completes, summary printed, scores visible in Langfuse UI

# Test 2: Check Langfuse
# Navigate to Datasets tab → verify dataset and experiment run are visible
# Navigate to Traces tab → verify evaluation traces are scored
```

**Automated Testing:**
```bash
dotnet test
```

**Success Criteria:**
- ✅ CLI supports `--eval` flag
- ✅ At least 8 diverse test scenarios defined
- ✅ Scores logged to Langfuse via REST API
- ✅ Dataset and experiment run visible in Langfuse UI
- ✅ JSON report generated with aggregate metrics
- ✅ Achieves >70% pass rate
- ✅ All tests pass

---

## Validation Checklist (Final)

After completing all steps, verify:

### Code Quality
- [ ] All unit tests pass (`dotnet test`)
- [ ] No deprecation warnings
- [ ] No linting errors/warnings
- [ ] XML comment blocks for all public methods
- [ ] EN-NZ spelling throughout

### Functionality
- [ ] Agent correctly identifies features from natural language
- [ ] Agent makes appropriate readiness decisions based on test results
- [ ] Agent handles missing/ambiguous data gracefully
- [ ] Agent provides clear reasoning with specific evidence
- [ ] Conversation history maintained correctly

### Observability (Langfuse)
- [ ] Traces appear in Langfuse for every conversation
- [ ] Traces include LLM calls, function calls, and decisions
- [ ] Token usage and timing data visible
- [ ] Can correlate conversation flow with trace spans
- [ ] Retry attempts visible in traces

### Evaluation (Langfuse)
- [ ] Evaluation suite has 8+ scenarios covering all acceptance criteria
- [ ] Scores logged to Langfuse via REST API
- [ ] Dataset and experiment visible in Langfuse UI
- [ ] Can run evaluations via CLI (`dotnet run -- --eval`)
- [ ] Achieves >70% pass rate
- [ ] JSON reports generated for analysis

### Documentation
- [ ] README.md explains setup and usage
- [ ] .env.example documents all config variables
- [ ] Code is self-documenting with clear naming
- [ ] Comments explain "why" not "what"

---

## Common Pitfalls to Avoid

1. **Skipping validation steps:** Always verify each step works before moving on
2. **Ignoring test failures:** Never commit failing tests or suppress warnings
3. **Copy-pasting code:** Understand what you're building; ask questions
4. **Over-engineering:** Keep it simple; follow YAGNI (You Aren't Gonna Need It)
5. **Forgetting to test manually:** Automated tests are necessary but not sufficient
6. **Not checking Langfuse:** Traces reveal what's actually happening
7. **Rushing through prompts:** System prompt quality directly impacts agent quality

---

## Success Criteria Summary

The Investigator Agent is complete when:

- ✅ Correctly identifies features from natural language descriptions
- ✅ Makes appropriate phase progression decisions based on test results
- ✅ Handles missing/ambiguous data gracefully with helpful error messages
- ✅ Provides clear reasoning for all decisions with specific evidence
- ✅ Includes comprehensive observability via Langfuse (OpenTelemetry traces)
- ✅ Demonstrates retry logic with exponential back-off
- ✅ Passes automated evaluation suite with >70% accuracy (tracked in Langfuse)
- ✅ All code quality standards met (no failing tests, warnings, or linting errors)

**Remember:** Quality over speed. It's better to have working, well-tested code than to rush through and create technical debt.
