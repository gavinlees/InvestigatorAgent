# Investigator Agent Implementation Steps

### Quick Start
- **Step 1**: UI setup, agent configuration, and basic conversation
- **Step 2**: Feature lookup tool & error handling/persistence
- **Step 3**: Testing results tool
- **Step 4**: Observability tracing
- **Step 5**: Retry with exponential back-off
- **Step 6**: Evaluations

---

## Step 1: UI Setup, Agent Configuration & Conversations

**Goal:** Set up a UI to interact with the agent and configure its behavior

**What You'll Build:**
- UI for testing the agent (web interface, TUI, or CLI)
- Agent configuration (prompt, temperature, model selection)
- Basic agent that responds to questions about features
- Simple conversation loop

**Success Criteria:**
- UI allows input and displays agent responses
- Agent can be configured (prompt, temperature, etc.)
- Agent responds to "What do you do?"
- Agent asks clarifying questions for vague feature names
- Basic personality and purpose are clear from responses
- Agent explains it assesses features for phase progression (Dev→UAT→Production)

**Test Conversation:**
```
You: What do you do?
Agent: I help assess whether software features are ready to progress through the development pipeline - from Development to UAT, or from UAT to Production. I analyze feature metadata and test results to provide readiness recommendations.

You: Is the payment feature ready?
Agent: I'd be happy to investigate the payment feature for you. However, I need more specific information to identify the exact feature. Could you provide the feature name, Jira ticket number, or any other identifying details?
```

---

## Step 2: Feature Lookup Tool & Error Handling/Persistence

**Goal:** Agent looks up feature metadata with proper error handling and saves conversations to disk

**What You'll Build:**
- Feature metadata lookup tool
- Error handling for missing/ambiguous features
- Conversation persistence to filesystem

**Success Criteria:**
- Agent successfully calls feature metadata lookup tool
- Handles missing features with helpful error messages
- Handles ambiguous feature names by asking for clarification
- Agent maintains conversation state in memory (remembers previous messages in session)
- Conversations are saved to filesystem as JSON files after each turn
- Saved conversations include: system prompt, user messages, agent responses, tool calls, and tool results
- Conversation files are human-readable and machine-parseable for later analysis

**Test Conversation:**
```
You: Is the maintenance scheduling feature ready for its next phase?
Agent: Let me look up the maintenance scheduling feature for you.

[Tool: feature_metadata_lookup("maintenance scheduling")]
Found: PLAT-1523 - Maintenance Scheduling & Alert System (Status: UAT)

I found the feature, but I'll need to check the test results before making a recommendation.

[Note: This conversation is being saved to conversations/conv_20251116_143022_a3f8b.json]
```

**Conversation Persistence Details:**
- Each CLI/UI session creates a new conversation file
- Files are saved incrementally after each turn (not just at session end)
- Format: `conversations/conv_YYYYMMdd_HHmmss_xxxxx.json`
- Structure includes: conversation_id, system_prompt, metadata (model, temperature), and complete turn history
- Each turn captures: user message, agent response, tool calls with arguments, tool results, timestamps
- Useful for: troubleshooting agent behavior, training data collection, performance analysis, debugging tool usage

---

## Step 3: Testing Metrics Results Tool

**Goal:** Agent analyzes test metric results to make readiness decisions

**What You'll Build:**
- Test metrics lookup tool
- Decision logic: if ANY tests are failing, recommend NOT progressing to next phase
- Clear reasoning in responses

**Success Criteria:**
- Agent successfully calls test metrics lookup tool
- Makes ready/not-ready decisions based on test results
- Shows tool results in conversation
- **Decision rule: progress to next phase IF AND ONLY IF no failing tests**
- Provides specific reasoning about test failures when present
- **Meets all acceptance criteria from DESIGN.md**

**Test Conversations:**

*Feature with Failing Tests:*
```
You: Is the QR check-in feature ready for its next phase?
Agent: I found the QR code check-in feature (FEAT-QR-002) currently in Development status.

[Tool: test_metrics_lookup("FEAT-QR-002")]
Test Results: 485/487 tests passing, 2 failures

❌ **NOT READY FOR UAT** - Feature has failing tests.

**Blockers:**
- 2 failed tests out of 487 total
- Race condition test failure
- Timezone bug in QR expiry logic

**Recommendation:** Fix all failing tests before progressing to UAT.
```

*Feature with All Tests Passing:*
```
You: Is the maintenance scheduling feature ready for its next phase?
Agent: I found the maintenance scheduling feature (FEAT-MS-001) currently in UAT status.

[Tool: test_metrics_lookup("FEAT-MS-001")]
Test Results: 156/156 tests passing, 0 failures

✅ **READY FOR PRODUCTION** - All tests passing.

**Recommendation:** Feature is ready to progress to production.
```

---

## Step 4: Observability Tracing

**Goal:** Add comprehensive observability with OpenTelemetry

**What You'll Build:**
- OpenTelemetry tracing for all agent operations
- Trace spans for tool calls, decisions, conversations
- JSON trace export to filesystem
- Trace correlation across operations

**Success Criteria:**
- Every conversation generates a complete trace
- Tool calls tracked with timing and parameters
- Decision-making logic captured in spans
- Traces saved as human-readable JSON files
- Can correlate conversation flow with trace spans

**Observability Output:**
```json
{
  "trace_id": "abc123...",
  "conversation_id": "conv_456",
  "spans": [
    {
      "name": "agent.investigate_feature",
      "duration_ms": 2341,
      "attributes": {
        "user_message": "Is QR check-in ready?",
        "feature_identified": "FEAT-QR-002",
        "current_phase": "development",
        "next_phase": "uat",
        "decision": "not_ready",
        "blocking_tests": 2,
        "agent_response": "❌ NOT READY FOR UAT - Critical issues...",
        "tools_called": ["feature_metadata_lookup", "test_metrics_lookup"]
      },
      "children": [
        {
          "name": "tool.feature_metadata_lookup",
          "duration_ms": 145,
          "attributes": {
            "query": "QR check-in",
            "matches_found": 1,
            "feature_id": "FEAT-QR-002"
          }
        },
        {
          "name": "tool.test_metrics_lookup", 
          "duration_ms": 89,
          "attributes": {
            "feature_id": "FEAT-QR-002",
            "total_tests": 487,
            "failed_tests": 2,
            "high_severity_failures": 1
          }
        }
      ]
    }
  ]
}
```

---

## Step 5: Retry with Exponential Back-off

**Goal:** Implement retry mechanisms for tool failures

**What You'll Build:**
- Retry logic with exponential backoff for tool failures
- Exception handling with graceful degradation

**Success Criteria:**
- Tool failures trigger automatic retries (up to 3 attempts)
- Exponential backoff applied between retries
- Exceptions are handled gracefully with helpful messages
- Retry attempts are logged in traces

**Error Handling Example:**
```
You: Is the inventory feature ready?
Agent: I'm having trouble accessing the feature database right now. Let me try again...

[Retry attempt 1/3]
[Retry attempt 2/3 - exponential backoff applied]

I was able to retrieve partial information. Based on what I can see, the inventory feature (FEAT-INV-003) appears to be in Development, but I couldn't access complete test results due to a temporary system issue. 

Would you like me to try again in a moment, or can you provide the feature ID directly?
```

---

## Step 6: Evaluations

**Goal:** Automated evaluation and performance tracking

**What You'll Build:**
- Automated evaluation suite with test scenarios covering all acceptance criteria
- Multiple evaluation dimensions (accuracy, tool usage, reasoning quality)
- Performance baselines and regression tracking
- Structured evaluation reports

**Success Criteria:**
- Evaluation suite tests all acceptance criteria from DESIGN.md
- Tracks accuracy of feature identification, phase decisions, and reasoning quality
- Detects performance regressions automatically
- Generates actionable improvement recommendations
- Achieves >85% success rate across all dimensions

**Evaluation Scenarios:**
- **Happy Path**: Clear feature names, complete data, obvious decisions
- **Ambiguous Queries**: Partial feature names requiring clarification
- **Edge Cases**: Missing test data, features in unexpected phases
- **Complex Decisions**: Borderline cases requiring nuanced judgment
- **Error Scenarios**: Tool failures, malformed data, invalid requests

**Evaluation Report:**
```json
{
  "summary": {
    "overall_score": 0.87,
    "pass_rate": 0.92,
    "total_scenarios": 25,
    "acceptance_criteria_met": true
  },
  "dimensions": {
    "feature_identification": 0.95,
    "phase_readiness_decisions": 0.82,
    "tool_usage_correctness": 0.91,
    "edge_case_handling": 0.79,
    "reasoning_quality": 0.85
  },
  "acceptance_criteria_results": {
    "basic_functionality": "PASS",
    "feature_matching_errors": "PASS", 
    "metadata_integration": "PASS",
    "test_driven_decisions": "PASS"
  },
  "regression_analysis": {
    "vs_baseline": "+0.03",
    "trending": "improving"
  }
}
```

---

## Success Progression

**After Step 1:** You have a UI and a talking agent ✅
**After Step 2:** You have feature lookup with error handling ✅  
**After Step 3:** You have a complete decision-maker that meets all acceptance criteria ✅
**After Step 4:** You have production observability ✅
**After Step 5:** You have robust retry logic ✅
**After Step 6:** You have a measurable, improvable agent ✅

Each step builds incrementally while maintaining working functionality. The acceptance criteria from DESIGN.md should be fully met by the end of Step 3, with Steps 4-6 adding production-grade capabilities.
