# Investigator Agent Design Specification

## Use Case: Feature Readiness Assessment

**System Context:** Modern software development teams deploy features through multiple environments in a progression: Development → User Acceptance Testing (UAT) → Production. Each transition requires careful assessment of feature stability, test results, and risk factors.

**Agent Role:** The Investigator Agent serves as an automated assessor, analysing feature metadata and test metrics to provide readiness recommendations for any phase transition in the pipeline.

**Scenario:**
When a product manager asks "Is the reservation feature ready for its next phase?", the agent:
1. **Looks up feature metadata** - retrieves Jira ticket, GitHub PR, current status, and context
2. **Analyses test metrics** - examines unit test results, coverage, failures, and blockers  
3. **Makes recommendation** - provides clear guidance with reasoning for the next appropriate phase

**Example Interactions:**

*Development → UAT Assessment:*
```
User: "Is the reservation feature ready for its next phase?"

Agent: I found the reservation feature (FEAT-MS-001) currently in Development status.

✅ **READY FOR UAT** - Feature meets criteria for User Acceptance Testing.

**Analysis:**
- All 156 unit tests passing (100% success rate)
- Code review completed and PR merged
- Security review shows LOW risk
- All stakeholder approvals obtained

**Recommendation:** Move to UAT environment for user testing.
```

*UAT → Production Assessment:*
```
User: "Is the QR code check-in feature ready for production?"

Agent: I found the QR code check-in feature (FEAT-QR-002) currently in UAT status.

❌ **NOT READY FOR PRODUCTION** - Critical issues prevent production deployment.

**Key Blockers:**
- 2 failed tests including HIGH severity race condition
- Critical bug: Multiple users can check in with same QR code simultaneously
- Second issue with timezone handling in QR expiry logic

**Test Results:** 485/487 tests passing (99.59%) - normally excellent, but the 2 failures are blockers.

**Recommendation:** Fix PLAT-1823 (race condition) and PLAT-1891 (timezone bug) before production deployment.
```

## Acceptance Criteria

Your implementation must satisfy these specific requirements to be considered complete:

### Core Functionality
- **GIVEN** the agent can be initialized without incident,
  - **WHEN** "Is the reservation feature ready for its next phase?" is submitted in the input field,
  - **THEN** the agent should respond with the feature's correct status and next phase recommendation.

### Feature Matching & Error Handling
- **GIVEN** the agent has access to some features,
  - **WHEN** a feature that should not match is submitted,
  - **THEN** the response should say it can't find a feature that confidently matches that description
  - **AND** the response should ask the user for more details about the feature

### Feature Lookup
- **GIVEN** an agent can successfully retrieve feature metadata
  - **WHEN** a request is for a feature that exists
  - **THEN** the response should include correct details about the feature's current status and context

### Decision Making
- **GIVEN** an agent can successfully retrieve feature test metrics
  - **WHEN** there are any failing tests
  - **THEN** the agent should recommend the feature not progress to the next phase
  - **AND** provide specific reasoning about test failure impact
- **GIVEN** an agent can successfully retrieve feature test metrics
  - **WHEN** there are no failing tests
  - **THEN** the agent should recommend progression to the next phase

## Agent Architecture

### Core Workflow
```
User Query → Feature Identification → Metadata Lookup → Test Analysis → Phase Readiness Decision
```

### Required Tools
1. **Feature Metadata Lookup** - Retrieves feature information from Jira, GitHub, planning docs, current status
2. **Test Metrics Lookup** - Analyses unit tests, integration tests, coverage, failure details, and severity

## Success Metrics

The agent is complete when it:
- ✅ Correctly identifies features from natural language descriptions
- ✅ Makes appropriate phase progression decisions based on current status and test results
- ✅ Handles missing/ambiguous data gracefully with helpful error messages
- ✅ Provides clear reasoning for all decisions with specific evidence
- ✅ Includes comprehensive observability and error handling
- ✅ Passes automated evaluation suite with >85% accuracy across all acceptance criteria
- ✅ Demonstrates retry logic and conversation persistence to filesystem (for troubleshooting, training, and analysis)
