# CLAUDE.md

You are an AI pair‑developer working inside this repository. Follow these instructions exactly.

## Project in one paragraph

A .NET application that lets users run natural‑language queries against Jira. An LLM turns NL prompts into structured intent, which a simple JQL builder converts to base JQL queries. The system emphasizes clean separation: simple, reliable JQL generation for data extraction, with sophisticated post-processing capabilities for analysis, grouping, and calculations. Current UI is a console CLI. The core is UI‑agnostic so web APIs or other front ends can be added later. For local dev and tests, mocks simulate the LLM and Jira.

## Goals

* Translate natural language → structured intent → base JQL reliably and simply.
* Keep LLM providers swappable behind an interface (Mock + OpenAI).
* Separate basic JQL generation from complex data processing.
* Enable offline, deterministic testing via mocks.
* Keep the core independent of any specific UI.

## Non‑goals

* Complex JQL generation logic (keep simple, move complexity to post-processing).
* Multiple LLM provider complexity (simplified to Mock + OpenAI only).
* Tight coupling to specific frameworks.

## Architecture snapshot

* **Domain model:** Minimal `Issue`, `QueryIntent`, and `QueryResult` types for core Jira operations.
* **LLM provider abstraction:** `ILlmClient` interface with Mock and OpenAI implementations only.
* **Simple JQL builder:** Direct conversion from `QueryIntent` to base JQL without complex handler patterns.
* **Unified post-processing:** Single processor handles sorting, grouping, calculations, and aggregations after data retrieval.
* **Front ends:** Console CLI now. Core is UI‑agnostic for future web APIs.
* **Mocks:** In‑memory Jira dataset and deterministic LLM fixtures for tests and local dev.

## Source layout (current)

```
src/
  Core/
    Domain/               # Issue.cs, QueryIntent.cs, QueryResult.cs
    Jql/                  # SimpleJqlBuilder.cs - core JQL generation
    Processing/           # UnifiedProcessor.cs - post-processing pipeline
    Llm/                  # ILlmClient, MockLlmClient, OpenAILlmClient
    Jira/                 # IJiraClient, JiraApiClient, MockJiraClient
    Output/               # OutputFormatters.cs - table, json, summary
  Cli/                    # Program.cs - console entry point
fixtures/                 # Sample Jira issues (in mock clients)
tests/
  Core.Tests/             # Unit tests for core components
```

## Runtime and configuration

* Requires .NET SDK installed. Use the SDK version already set in `global.json` if present.
* Configure via environment variables (preferred):

  * `JIRA_BASE_URL`, `JIRA_EMAIL`, `JIRA_TOKEN`
  * `LLM_PROVIDER` = `OpenAI` | `Mock` (default: Mock)
  * `LLM_API_KEY` when provider = OpenAI
  * `USE_MOCKS` = `true` to force offline mode (default: true)

### Quickstart

```bash
# build
dotnet build

# run CLI in mock mode (no network)
USE_MOCKS=true dotnet run --project src/Cli -- "show bugs assigned to me updated last 7 days"

# run with post-processing
USE_MOCKS=true dotnet run --project src/Cli -- "open issues" --group-by status --format json

# run tests
dotnet test
```

## LLM I/O contract

**Input to LLM:**
* System prompt explaining Jira fields and required JSON output schema.
* Few‑shot examples covering basic filters: project, assignee, status, type, date ranges, text search, sorting.

**Expected output from LLM:** Simple JSON intent object:

```json
{
  "filters": {
    "project": "BANK",
    "assignee": "currentUser",
    "status": ["In Progress", "Open"],
    "updated": { "lastDays": 7 },
    "issueTypes": ["Bug"]
  },
  "search": "payment bug",
  "sort": [{ "field": "updated", "order": "desc" }],
  "limit": 50
}
```

**Key simplification:** No aggregations in intent - they're handled in post-processing.

## JQL generation approach

The `SimpleJqlBuilder` converts `QueryIntent` directly to JQL:

* **Single class** replaces complex handler registry pattern.
* **Direct mapping:** Each filter type maps to specific JQL syntax.
* **Pure functions:** Each `Add*Filter` method is stateless and predictable.
* **Simple composition:** Combine clauses with AND operators.

### Adding a new filter type

1. Add field to `QueryFilters` record in `Domain/QueryIntent.cs`.
2. Add `Add*Filter` method in `SimpleJqlBuilder.cs`.
3. Call the method from `BuildQuery`.
4. Add unit tests in `SimpleJqlBuilderTests.cs`.

Example:
```csharp
// 1. Add to QueryFilters
string? Epic = null

// 2. Add filter method
private static void AddEpicFilter(List<string> whereClauses, string? epic)
{
    if (!string.IsNullOrEmpty(epic))
        whereClauses.Add($"\"Epic Link\" = \"{epic}\"");
}

// 3. Call in BuildQuery
AddEpicFilter(whereClauses, intent.Filters.Epic);
```

## Post-processing pipeline

The `UnifiedProcessor` handles complex operations **after** basic data retrieval:

* **Sorting:** Client-side sorting by any field (moved from JQL).
* **Grouping:** Group by status, assignee, priority, etc.
* **Calculations:** Age, velocity, status distribution.
* **Aggregations:** Counts, averages, breakdowns.

### Adding new post-processing features

Add to `UnifiedProcessor.ApplyCalculations` or `ApplyAggregations`:

```csharp
"complexity" => CalculateComplexity(issues),

private static object CalculateComplexity(IReadOnlyList<Issue> issues)
{
    return issues.GroupBy(i => i.Type)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());
}
```

## Testing strategy

* **Unit tests:** Test `SimpleJqlBuilder` and `UnifiedProcessor` with known inputs.
* **Integration tests:** Test full pipeline with mock LLM responses.
* **No external calls:** All tests use mock providers.
* **Deterministic:** Mock data provides predictable results.

## CLI usage patterns

```bash
# Basic queries
USE_MOCKS=true dotnet run --project src/Cli -- "bugs assigned to me"

# With post-processing
USE_MOCKS=true dotnet run --project src/Cli -- "open issues" --group-by status --calculate age

# Different output formats
USE_MOCKS=true dotnet run --project src/Cli -- "issues in BANK project" --format json
USE_MOCKS=true dotnet run --project src/Cli -- "recent updates" --format summary

# Debug mode
USE_MOCKS=true dotnet run --project src/Cli -- "your query" --debug
```

## Extensibility guidelines

* **JQL filters:** Add directly to `SimpleJqlBuilder` - no complex abstractions needed.
* **Post-processing:** Extend `UnifiedProcessor` with new calculations/aggregations.
* **Output formats:** Implement `IOutputFormatter` interface.
* **LLM providers:** Implement `ILlmClient` (keep simple - avoid complex configuration).

## Security and privacy

* Never log API keys, tokens, or user-identifiable data.
* Mock data contains only synthetic information.
* Real JIRA connections require proper authentication.

## Coding conventions

* Use existing patterns: records for domain types, dependency injection, async/await.
* Keep methods pure and testable.
* Nullable reference types enabled.
* Minimal abstractions - prefer simple, direct code.

## What Claude should do by default

* Follow the simplified architecture - don't reintroduce complex patterns.
* Add tests for any new functionality.
* Keep JQL generation simple and move complexity to post-processing.
* Preserve the separation between basic data retrieval and analysis.

## Simplified development approach

* **For basic filters:** Modify `SimpleJqlBuilder` directly.
* **For data analysis:** Extend `UnifiedProcessor`.
* **For output:** Create new formatters.
* **For testing:** Use mock clients with synthetic data.

The architecture emphasizes **simplicity and separation of concerns**: reliable base JQL generation for data extraction, powerful post-processing for analysis.