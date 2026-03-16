# Module 7 Project: Investigator Agent

**System Context:**

The Investigator Agent is part of the Release Confidence System team that helps build confidence in software releases by investigating changes, assessing risks, and producing actionable reports. This agent inspects the state of a feature and determines if it is sufficiently prepared to be promoted to the next stage of development. For example, from the Development environment to UAT, or from UAT to production.

Your task is to build the Investigator Agent. What follows is the agent's acceptance criteria. The agent can be built using any tools you like. Given you built an agent from scratch last week, we recommend you use a popular SDK, such as LangChain, PydanticAI, and CrewAI, or even a no-code solution such as n8n.

If you're following our recommendation, you'll find instructions for using `DESIGN.md` and `STEPS.md` after the acceptance criteria that will (hopefully) make things easier for you.

## Prerequisites

Before starting this module, you should have completed Module 6 (Detective Agent) with working implementations of:

- ✅ Basic conversation loop with LLM provider
- ✅ Provider abstraction (at least one provider working)
- ✅ Observability (traces and spans with OpenTelemetry)
- ✅ Retry mechanism with exponential backoff
- ✅ System prompt engineering
- ✅ Tool abstraction and execution loop
- ✅ Basic evaluation framework

## Acceptance Criteria

### Can identify features and retrieve metadata
**Acceptance Criteria:**
- JIRA tool retrieves metadata for ALL features (no parameters)
- Returns array with: folder, jira_key, feature_id, summary, status, data_quality
- Agent identifies which feature user is asking about from natural language query
- Agent extracts feature_id for subsequent tool calls
- Feature ID → folder mapping utility created
- Tool handles missing JIRA file gracefully with clear error messages
- Feature metadata visible in conversation and traces
- Automated tests verify tool behavior
- Manual CLI test demonstrates feature identification for all 4 features

### Can analyze features using analysis data
**Acceptance Criteria:**
- Analysis tool retrieves different types of analysis data (metrics + reviews):
  - metrics/performance_benchmarks: Performance benchmarks
  - metrics/pipeline_results: Pipeline run results
  - metrics/security_scan_results: Security scan results
  - metrics/test_coverage_report: Test coverage statistics
  - metrics/unit_test_results: Unit test results
  - reviews/security: Security review results
  - reviews/stakeholders: Stakeholder reviews
  - reviews/uat: UAT environment test results
- Agent uses feature ID → folder mapping for analysis queries
- Agent calls multiple analysis types during assessment
- Tool handles missing analysis files gracefully
- Analysis data incorporated into decision-making
- Automated tests verify all 8 analysis types
- Manual CLI test shows multi-analysis assessment

### Can be evaluated
**Evaluation Dimensions:**
1. Feature Identification Evaluation
2. Tool Usage Evaluation
3. Decision Quality Evaluation
4. Error Handling Evaluation

**Evaluation Acceptance Criteria:**
- Eval framework runs test scenarios automatically
- At least 8 test scenarios covering:
  - Ready for promotion (all metrics green)
  - Not ready (clear failures)
  - Borderline cases (judgment required)
  - Error handling (missing data, bad files)
  - Context management (large docs)
- Feature identification evaluated for correctness
- Tool usage evaluated for correctness and ordering
- Decision quality measured against expected outcomes
- Error handling scenarios validate robustness
- Structured JSON reports generated for automation
- Eval results include pass/fail and diagnostic details
- At least 70% of scenarios pass
- Automated tests verify eval framework itself
- Documentation explains how to add new eval cases
- CLI supports baseline establishment and comparison

### Observability
**Additional Observability Acceptance Criteria:**
- Conversation spans captured in traces
- All tools (JIRA, and metrics) traced with timing
- Trace files show complete workflow including tool calls
- Can correlate tool calls to parent via trace IDs

### Has clear purpose and behavior
**Prompt Engineering Acceptance Criteria:**
- System prompt defines Investigator Agent role
- System prompt explains feature readiness assessment task
- System prompt describes all available tools and when to use them
- System prompt includes decision criteria (test thresholds, required docs, etc.)
- System prompt explains sub-conversation behavior
- Agent behavior reflects system prompt instructions
- Tested with various features and scenarios

## Building an agent using an SDK

This process has 2 main parts:
1. Create the implementation plan
2. Iteratively follow the steps in the plan

Everything you and your assistant need to know to implement the Investigator Agent is in `DESIGN.md`. We also recommend a specific order when adding features to your agent. Inside `STEPS.md` you'll find an order of implementation that mirrors the acceptance criteria above and builds incrementally on your Module 6 Detective Agent.

We've also provided `PLAN.md` which is an example starter file that a Python developer might use. Feel free to edit this file according to your preferences and language choice. Once you're happy with it, you're ready to begin.

## Test Data

**Important:** The test data is already provided in the `incoming_data/` directory. You do NOT need to create it.

The test data includes **4 features** with different readiness scenarios:

| Feature | Complexity | Stage | Readiness | Description |
|---------|-----------|-------|-----------|-------------|
| **Maintenance Scheduling & Alert System** | Medium | Production-Ready | ✅ READY | Clear success case - all gates passed, complete docs, green metrics |
| **QR Code Check-in/out with Mobile App** | High | Development/UAT | ❌ NOT READY | Multiple blockers - clear failures in testing and implementation |
| **Advanced Resource Reservation System** | High | UAT | 🔄 AMBIGUOUS | Mixed signals - incomplete data, requires judgment |
| **Contribution Tracking & Community Credits** | Medium-High | UAT | 🔄 PARTIAL | Right stage but not ready for next - some gaps remain |

Each feature in `incoming_data/featureN/` includes:
- `jira/`: JIRA metadata (feature_issue.json, issue_changelog.json)
- `metrics/`: 5 different JSON metric files
  - performance_benchmarks.json
  - pipeline_results.json
  - security_scan_results.json
  - test_coverage_report.json
  - unit_test_results.json
- `reviews/`: 3 different JSON review files
  - security.json
  - stakeholders.json
  - uat.json
- `planning/`: Multiple markdown documentation files (10+ files per feature)
  - ARCHITECTURE.md (large, >10KB)
  - DESIGN_DOC.md (large, >10KB)
  - DATABASE_SCHEMA.md (large)
  - DEPLOYMENT_PLAN.md, TEST_STRATEGY.md, USER_STORIES.md, etc.

**Total data size:** Approximately 1+ MB across all features

The data is realistic and specifically designed to challenge the agent's decision-making and context management capabilities.

## Steps

### Step 0: Verify Detective Agent Foundation

Before starting, ensure your Module 6 Detective Agent is working:

```bash
# Run existing tests
pytest

# Test basic conversation
python cli.py

# Verify traces are being generated
ls traces/
```

### Step 1: Create the implementation plan

The first step is to create the plan you're going to use going forward. Here is an example prompt you can use to create it. Feel free to modify it before using it.

```
Read @PLAN.md and follow your additional instructions inside it. I would like to use OpenRouter as the first provider.
```

Review the plan it created:
- Are the tasks broken down to sufficient detail to avoid overwhelm during implementation?
- Does it follow the same STEPS order?
- Did it add or remove any sections?
- Briefly look over the code - anything unusual?

You may need to collaborate with your assistant to get the planning doc where you're happy with it.

### Step 2: Iterate over the steps in the plan

This is where you get to decide how much you want to bite off at a time. Hopefully the plan is already broken down well enough. When you're considering the next piece of work, if it doesn't have enough detail, or if it needs to be broken down further, work with your assistant to update PLAN.md until you're happy with what you know it's going to build, or happy enough with the mystery ;)

Here is an example prompt that is a good one to start each step with. Sometimes it may be enough for your assistant to finish it, sometimes it will require some back and forth. Start over if it's too far off at the beginning.

#### Remember to: START EACH STEP WITH A NEW ASSISTANT SESSION

```
Please complete step 1.1 in @PLAN.md. Refer to the appropriate section in @DESIGN.md to ensure you're meeting all of the acceptance criteria.
```

Don't be satisfied with all green on your test run. Test it manually. Make sure you can see what you expect to see in the traces (responses, errors, tool calls, sub-conversations, context compression, memory operations, etc). Have your assistant create CLI interfaces so you can _verify_ everything is actually working.

#### Remember to: STAGE/COMMIT THE WORKING TREE

After each major step, commit your working code:

```bash
git add .
git commit -m "Step X.Y: [description]"
```

This gives you safe rollback points if needed.

### Step-by-Step Workflow

We recommend using the progression detailed in `STEPS.md`.

### Verification Guidelines

For each step:

- ✅ **Run automated tests** - Ensure code quality
- ✅ **Run manual CLI tests** - Verify actual behavior
- ✅ **Check traces** - Ensure observability is capturing everything
- ✅ **Test edge cases** - Missing data, errors, large files

Don't move to the next step until the current step is fully working.

### When You're Done

Repeat until Investigator Agent is ready for duty:

> _All of the acceptance criteria are met_

The agent should be able to:
- Accept natural language queries about feature readiness
- Identify the feature and gather all relevant data (JIRA, metrics, docs)
- Manage context effectively using sub-conversations for large content
- Make sound decisions with clear justifications
- Remember past assessments (if memory implemented)
- Pass comprehensive evaluations (>70% success rate)

:)

## Tips for Success

1. **Build incrementally** - Each step adds one capability
2. **Verify thoroughly** - Don't skip manual testing
3. **Watch your tokens** - Context management is critical here
4. **Use traces** - They show what's really happening
5. **Commit often** - Safe rollback points are invaluable
6. **Test with real scenarios** - The test data should challenge the agent

## Key Learnings from This Module

By completing this exercise, you'll learn:

- **Context Engineering**: How to manage large information spaces without exhausting context windows
- **Sub-Conversation Patterns**: A critical technique for complex agent workflows
- **Information Synthesis**: How agents can analyze multiple data sources and make decisions
- **Memory Integration**: How to give agents long-term memory (optional)
- **Comprehensive Evaluation**: How to test agents across multiple dimensions
- **Real-world Agent Design**: Practical patterns for production agent systems

Good luck!
