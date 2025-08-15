# JQLBridge

A .NET application that translates natural language queries into Jira Query Language (JQL) using Large Language Models.

## Overview

JQLBridge allows users to query Jira issues using natural language instead of learning JQL syntax. Simply describe what you're looking for in plain English, and JQLBridge will convert it to the appropriate JQL query and return formatted results.

**Example:**
```bash
dotnet run --project src/Cli -- "show bugs assigned to me updated last 7 days"
```

**Output:**
```
Generated JQL: (assignee = currentUser()) AND (type = "Bug") AND (updated >= -7d)

┌──────────┬─────────────────────────────────┬────────┬──────────┬────────────┐
│ Key      │ Summary                         │ Status │ Assignee │ Updated    │
├──────────┼─────────────────────────────────┼────────┼──────────┼────────────┤
│ BANK-123 │ Payment processing bug in...    │ Open   │ John Doe │ 2025-08-14 │
└──────────┴─────────────────────────────────┴────────┴──────────┴────────────┘
```

## Features

- 🗣️ **Natural Language Processing**: Convert plain English to JQL
- 🔌 **Provider Agnostic**: Swappable LLM providers (OpenAI, Anthropic, Mock)
- 🧩 **Extensible Handlers**: Add new query types via composable handlers
- 🎯 **Offline Development**: Mock providers for testing without external APIs
- 📊 **Rich CLI Output**: Beautiful tables with Spectre.Console
- ✅ **Comprehensive Tests**: Full unit test coverage
- 🏗️ **Clean Architecture**: UI-agnostic core for easy frontend additions

## Quick Start

### Prerequisites
- .NET 8.0 SDK
- Git

### Installation

```bash
git clone https://github.com/alun1111/JQLBridge.git
cd JQLBridge
dotnet build
```

### Usage

#### Mock Mode (Offline Development)
```bash
# Set environment variable for mock mode
export USE_MOCKS=true

# Or prefix the command
USE_MOCKS=true dotnet run --project src/Cli -- "your natural language query"
```

#### Example Queries
```bash
# Find bugs assigned to you
USE_MOCKS=true dotnet run --project src/Cli -- "bugs assigned to me"

# High priority stories in a specific project
USE_MOCKS=true dotnet run --project src/Cli -- "high priority stories in BANK project"

# Recently updated issues
USE_MOCKS=true dotnet run --project src/Cli -- "open issues updated last week"

# Unassigned bugs
USE_MOCKS=true dotnet run --project src/Cli -- "unassigned bugs"
```

### Running Tests

```bash
dotnet test
```

## Configuration

JQLBridge can be configured via environment variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `USE_MOCKS` | Use mock providers for offline development | `true` |
| `LLM_PROVIDER` | LLM provider to use (`Mock`, `OpenAI`, `Anthropic`) | `Mock` |
| `LLM_API_KEY` | API key for LLM provider | - |
| `JIRA_BASE_URL` | Jira instance URL | - |
| `JIRA_EMAIL` | Jira user email | - |
| `JIRA_TOKEN` | Jira API token | - |

## Architecture

### Core Components

- **Domain Models**: `Issue`, `QueryIntent`, `QueryResult` - Clean data structures
- **LLM Abstraction**: `ILlmClient` - Provider-agnostic interface for language models
- **Query Engine**: Composable handlers that convert intents to JQL fragments
- **Jira Client**: `IJiraClient` - Abstraction for Jira API interactions
- **CLI Interface**: Spectre.Console-based command-line interface

### Project Structure

```
src/
├── Core/                           # Core business logic
│   ├── Domain/                     # Domain models and entities
│   ├── Llm/                        # LLM provider interfaces and implementations
│   ├── QueryEngine/                # Query processing engine
│   │   └── Handlers/               # Individual query handlers
│   └── Jira/                       # Jira client abstraction
└── Cli/                            # Console application
tests/
└── Core.Tests/                     # Unit tests
```

### Query Handlers

The system uses composable handlers for different query aspects:

- **AssigneeHandler**: `assignee = currentUser()`, `assignee = "john.doe"`
- **StatusHandler**: `status = "Open"`, `status IN ("Open", "In Progress")`
- **ProjectHandler**: `project = "PROJ"`
- **IssueTypeHandler**: `type = "Bug"`, `type IN ("Bug", "Story")`
- **DateRangeHandler**: `updated >= -7d`, `created >= "2024-01-01"`
- **SortHandler**: `ORDER BY updated DESC`

## Supported Query Patterns

| Natural Language | Generated JQL |
|-----------------|---------------|
| "bugs assigned to me" | `assignee = currentUser() AND type = "Bug"` |
| "high priority stories in BANK project" | `project = "BANK" AND priority = "High" AND type = "Story"` |
| "open issues updated last 7 days" | `status = "Open" AND updated >= -7d` |
| "unassigned bugs" | `assignee is EMPTY AND type = "Bug"` |

## Development

### Adding New Query Handlers

1. Create a new handler implementing `IQueryHandler`:

```csharp
public class PriorityHandler : IQueryHandler
{
    public string Name => "Priority";
    
    public bool CanHandle(QueryIntent intent)
    {
        return intent.Filters?.Priorities?.Any() == true;
    }
    
    public JqlFragment Handle(QueryIntent intent)
    {
        // Convert intent to JQL fragment
    }
}
```

2. Register the handler in `Program.cs`:

```csharp
registry.RegisterHandler(new PriorityHandler());
```

### Adding New LLM Providers

1. Implement the `ILlmClient` interface
2. Register in the DI container
3. Add configuration support

## Roadmap

- [ ] OpenAI GPT integration
- [ ] Anthropic Claude integration
- [ ] Real Jira API client
- [ ] Web API frontend
- [ ] Advanced query handlers (text search, custom fields)
- [ ] Query result aggregations
- [ ] JSON output format
- [ ] Configuration file support

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes with tests
4. Submit a pull request

## License

This project is open source. See the repository for license details.

## Support

For questions or issues, please create an issue on GitHub.