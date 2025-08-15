# CLAUDE.md

You are an AI pair‑developer working inside this repository. Follow these instructions exactly.

## Project in one paragraph

A .NET application that lets users run natural‑language queries against Jira. An LLM turns NL prompts into JQL through a provider‑agnostic interface. Core logic uses a small C# Jira issue model and a function‑based query engine (akin to Cucumber step definitions) so new query types and aggregations are added as reusable handlers. Current UI is a Spectre.Console CLI. The core is UI‑agnostic so a web API or other front ends can be added later. For local dev and tests, mocks simulate the LLM and Jira.

## Goals

* Translate natural language → structured intent → JQL reliably.
* Keep LLM providers swappable behind an interface.
* Make query handlers composable and easy to extend.
* Enable offline, deterministic testing via mocks.
* Keep the core independent of any specific UI or HTTP client.

## Non‑goals

* Hard‑coding query scenarios.
* Tight coupling to a single LLM or Jira client SDK.
* Hitting external APIs during unit tests.

## Architecture snapshot

* **Domain model:** Minimal `Issue` and related types for Jira fields used by queries.
* **LLM provider abstraction:** `ILLMClient` (name may differ) that accepts a prompt payload and returns a structured intent. Concrete adapters implement OpenAI, Anthropic, etc.
* **Query engine:** Registry of small, pure handler functions. Each handler maps a recognizable intent fragment to JQL fragments and optional aggregations. Handlers compose into full queries.
* **Front ends:** `cli/` using Spectre.Console now. Web API or other adapters can reuse the same core.
* **Mocks:** In‑memory Jira dataset and deterministic LLM fixtures for tests and local dev.

> If the exact type or folder names differ in this repo, mirror the existing names and patterns.

## Source layout (target shape)

```
src/
  Core/
    Domain/               # Issue.cs, Value Objects
    QueryEngine/          # Handlers, registry, composer
    Llm/                  # Interfaces + provider adapters
    Jira/                 # DTOs, JQL builder utilities
  Cli/                    # Spectre.Console entry point
fixtures/                 # Sample Jira issues, LLM outputs
tests/
  Core.Tests/
  Cli.Tests/
```

## Runtime and configuration

* Requires .NET SDK installed. Use the SDK version already set in `global.json` if present.
* Configure via environment variables (preferred) or user‑secrets:

  * `JIRA_BASE_URL`, `JIRA_EMAIL`, `JIRA_TOKEN`
  * `LLM_PROVIDER` = `OpenAI` | `Anthropic` | `Mock`
  * `LLM_API_KEY` when provider ≠ Mock
  * `USE_MOCKS` = `true` to force offline mode

### Quickstart

```bash
# build
dotnet build

# run CLI in mock mode (no network)
USE_MOCKS=true dotnet run --project src/Cli -- "show bugs assigned to me updated last 7 days"

# run tests
dotnet test
```

## LLM I/O contract (target)

Claude should preserve or align to this contract when editing code. If the repo already defines one, keep that as the source of truth.

**Input to LLM:**

* System prompt that explains Jira fields supported and the required output schema.
* Few‑shot examples covering filters, sorting, limits, date ranges, assignee, status, project, text search, and simple aggregations.

**Expected output from LLM:** a compact JSON intent object consumable by the query engine, for example:

```json
{
  "filters": {
    "project": "BANK",
    "assignee": "currentUser",
    "status": ["In Progress", "To Do"],
    "updated": { "lastDays": 7 }
  },
  "search": "payment bug",
  "sort": [{ "field": "updated", "order": "desc" }],
  "limit": 50,
  "aggregations": [{ "type": "count" }]
}
```

**Do not return JQL directly from the LLM.** The query engine converts intent → JQL safely.

## Query engine design rules

* Handlers are **pure** functions: `(IntentFragment) -> JqlFragment | Aggregation`.
* Register handlers in a central registry. No implicit side effects.
* Composition order: validation → intent normalization → handler mapping → JQL assembly → execution.
* New capability = new handler + tests + registry entry. No switch/case explosions.

### Adding a new query type

1. Define or extend the intent schema if required.
2. Implement a handler in `QueryEngine/Handlers/`.
3. Register it in the handler registry.
4. Add unit tests with mock data ensuring JQL output and results match expectations.
5. Add one few‑shot example to the LLM prompt fixtures when applicable.

## Testing strategy

* **Unit tests:** Cover handlers and JQL builder with deterministic inputs.
* **Fixture tests:** Given an NL prompt and a fixed mock LLM response, assert the composed JQL and filtered mock dataset.
* **No network in CI:** Default provider must be `Mock` under test.
* **Golden files** optional: store expected JQL per scenario.

## CLI guidelines (Spectre.Console)

* Commands accept a raw NL prompt string and optional flags: `--limit`, `--project`, `--mock`.
* Render tabular results. Truncate long text fields. Provide `--json` to emit machine‑readable output.
* Keep CLI thin; delegate to core services.

## Extensibility guidelines

* LLM providers implement the same interface and are selected by `LLM_PROVIDER`.
* Jira access behind a gateway. For tests, replace with an in‑memory repository.
* Prefer small PRs. One capability per PR. Include tests and docs updates.

## Security and privacy

* Never log API keys, tokens, or full prompts containing customer data.
* Redact user‑identifiable fields in logs.
* Mocks and fixtures must contain only synthetic data.

## Coding conventions

* Use existing repo conventions. If absent: nullable reference types on, `async` all I/O, guard clauses, no static singletons.
* Keep public interfaces stable. Add new methods instead of breaking changes.

## What Claude should do by default

* Propose diffs with minimal scope. Include filenames and exact patches.
* Explain design impact in 3–5 bullets.
* Add or update tests for any behavior change.
* If uncertain about a contract, inspect code and align with it rather than inventing new ones.

## Tasks safe to automate

* Add a new handler (e.g., "updated in last N days").
* Implement an Anthropic or OpenAI provider adapter behind the common interface.
* Add CLI switches for output formats and limits.
* Introduce fixtures and golden tests for common prompts.
* Create a basic web API adapter that forwards NL prompts and returns JSON results.

## Guardrails

* Do not call external services in unit tests.
* Do not bypass the provider abstraction.
* Do not hard‑code business logic that belongs in handlers.
* Do not couple core to Spectre.Console.

## Glossary

* **Intent**: Structured representation of a natural‑language request.
* **Handler**: Pure function mapping part of an intent to JQL or an aggregation.
* **Registry**: Collection that composes handlers to build final JQL.

---

### Maintainer notes

Replace any placeholders with the repo’s actual namespaces, folders, and interface names. Keep this file concise and actionable.
