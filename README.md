# JQLBridge

A .NET application that translates natural language queries into Jira Query Language (JQL) using Large Language Models, with powerful post-processing capabilities.

## Overview

JQLBridge allows users to query Jira issues using natural language instead of learning JQL syntax. The system emphasizes **simple, reliable JQL generation** for data extraction, with **sophisticated post-processing** for analysis, grouping, and calculations.

**Example:**
```bash
USE_MOCKS=true dotnet run --project src/Cli -- "show bugs assigned to me updated last 7 days"
```

**Output:**
```
Generated JQL: (assignee = currentUser()) AND (type = "Bug") AND (updated >= -7d)

┌──────────┬─────────────────────────────────┬────────────┬──────────────┬────────────┐
│ Key      │ Summary                         │ Status     │ Assignee     │ Updated    │
├──────────┼─────────────────────────────────┼────────────┼──────────────┼────────────┤
│ BANK-123 │ Payment processing bug in ch... │ Open       │ John Doe     │ 2025-08-19 │
└──────────┴─────────────────────────────────┴────────────┴──────────────┴────────────┘
```

## Features

- 🗣️ **Natural Language Processing**: Convert plain English to JQL
- 🔌 **Simple LLM Integration**: Mock and OpenAI providers (simplified from complex multi-provider setup)
- 🎯 **Separation of Concerns**: Simple JQL generation + powerful post-processing
- 📊 **Rich Post-Processing**: Grouping, calculations, aggregations, and multiple output formats
- 🧪 **Offline Development**: Mock providers for testing without external APIs
- ✅ **Comprehensive Tests**: Full unit test coverage with deterministic results
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
# Basic queries
USE_MOCKS=true dotnet run --project src/Cli -- "bugs assigned to me"

# With post-processing
USE_MOCKS=true dotnet run --project src/Cli -- "open issues" --group-by status --calculate age

# Different output formats
USE_MOCKS=true dotnet run --project src/Cli -- "issues in BANK project" --format json
USE_MOCKS=true dotnet run --project src/Cli -- "recent updates" --format summary
```

#### Example Queries
```bash
# Find bugs assigned to you
USE_MOCKS=true dotnet run --project src/Cli -- "bugs assigned to me"

# High priority stories in a specific project
USE_MOCKS=true dotnet run --project src/Cli -- "high priority stories in BANK project"

# Recently updated issues with grouping
USE_MOCKS=true dotnet run --project src/Cli -- "open issues updated last week" --group-by assignee

# Unassigned bugs with calculations
USE_MOCKS=true dotnet run --project src/Cli -- "unassigned bugs" --calculate age --aggregate count
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
| `LLM_PROVIDER` | LLM provider to use (`Mock`, `OpenAI`) | `Mock` |
| `LLM_API_KEY` | API key for LLM provider | - |
| `JIRA_BASE_URL` | Jira instance URL | - |
| `JIRA_EMAIL` | Jira user email | - |
| `JIRA_TOKEN` | Jira API token | - |

## Architecture

### Core Philosophy

JQLBridge follows a **simplified pipeline approach**:

```
Natural Language → QueryIntent → Base JQL → JIRA Data → Post-Processing → Output
```

**Key Principles:**
- **Simple JQL Generation**: Direct mapping from intent to base JQL without complex handler patterns
- **Powerful Post-Processing**: Complex operations happen after data retrieval
- **Clean Separation**: Basic data extraction vs. sophisticated analysis

### Core Components

- **Domain Models**: `Issue`, `QueryIntent`, `QueryResult` - Clean data structures
- **Simple JQL Builder**: `SimpleJqlBuilder` - Direct intent-to-JQL conversion
- **Unified Processor**: `UnifiedProcessor` - All post-processing in one place
- **LLM Abstraction**: `ILlmClient` - Simple interface with Mock and OpenAI implementations
- **Jira Client**: `IJiraClient` - Abstraction for Jira API interactions
- **Output Formatters**: Table, JSON, and Summary formats

### Project Structure

```
src/
├── Core/                           # Core business logic
│   ├── Domain/                     # Issue.cs, QueryIntent.cs, QueryResult.cs
│   ├── Jql/                        # SimpleJqlBuilder.cs - core JQL generation
│   ├── Processing/                 # UnifiedProcessor.cs - post-processing pipeline
│   ├── Llm/                        # ILlmClient, MockLlmClient, OpenAILlmClient
│   ├── Jira/                       # IJiraClient, JiraApiClient, MockJiraClient
│   └── Output/                     # OutputFormatters.cs - table, json, summary
└── Cli/                            # Program.cs - console entry point
tests/
└── Core.Tests/                     # Unit tests for core components
```

## Supported Query Patterns

| Natural Language | Generated JQL |
|-----------------|---------------|
| "bugs assigned to me" | `(assignee = currentUser()) AND (type = "Bug")` |
| "high priority stories in BANK project" | `(project = "BANK") AND (type = "Story") AND (priority = "High")` |
| "open issues updated last 7 days" | `(status IN ("Open", "To Do")) AND (updated >= -7d)` |
| "unassigned bugs" | `(assignee is EMPTY) AND (type = "Bug")` |

## Post-Processing Features

### Grouping
```bash
--group-by status          # Group by issue status
--group-by assignee        # Group by assignee
--group-by priority        # Group by priority level
```

### Calculations
```bash
--calculate age            # Calculate issue age in days
--calculate velocity       # Calculate team velocity
--calculate avgAge         # Calculate average issue age
--calculate statusdistribution  # Status distribution analysis
```

### Aggregations
```bash
--aggregate count          # Total issue count
--aggregate avg_age        # Average age across all issues
--aggregate status_counts  # Count by status
--aggregate assignee_counts # Count by assignee
```

### Output Formats
```bash
--format table            # Default table format
--format json             # Machine-readable JSON
--format summary          # Executive summary
```

## Development

### Adding New JQL Filters

To add a new filter (e.g., Epic), modify `SimpleJqlBuilder.cs`:

```csharp
// 1. Add to QueryFilters record in Domain/QueryIntent.cs
public record QueryFilters(
    // ... existing filters
    string? Epic = null);

// 2. Add filter method in SimpleJqlBuilder.cs
private static void AddEpicFilter(List<string> whereClauses, string? epic)
{
    if (!string.IsNullOrEmpty(epic))
    {
        whereClauses.Add($"\"Epic Link\" = \"{epic}\"");
    }
}

// 3. Call it in BuildQuery method
AddEpicFilter(whereClauses, intent.Filters.Epic);
```

### Adding New Post-Processing Features

To add new calculations/aggregations, extend `UnifiedProcessor.cs`:

```csharp
// Add to ApplyCalculations method
"complexity" => CalculateComplexity(issues),

// Implement the calculation
private static object CalculateComplexity(IReadOnlyList<Issue> issues)
{
    return issues.GroupBy(i => i.Type)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());
}
```

### Adding New Output Formats

Create new formatters in `Output/OutputFormatters.cs`:

```csharp
public class CsvFormatter : IOutputFormatter
{
    public string Name => "csv";
    
    public Task<string> FormatAsync(QueryResult result, ProcessingResult? processed = null)
    {
        // Implementation
    }
}
```

## Roadmap

### Completed ✅
- [x] Simplified JQL generation pipeline
- [x] OpenAI integration  
- [x] Real Jira API client
- [x] Advanced post-processing (grouping, calculations, aggregations)
- [x] Multiple output formats (JSON, table, summary)
- [x] Comprehensive test suite

### Future Enhancements
- [ ] Web API frontend
- [ ] Additional LLM providers (Claude, etc.)
- [ ] Custom field support
- [ ] Configuration file support
- [ ] Export capabilities (CSV, Excel)

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes with tests
4. Submit a pull request

### Development Guidelines
- Use mock mode (`USE_MOCKS=true`) for development
- Add tests for any new functionality
- Follow the simplified architecture patterns
- Keep JQL generation simple and move complexity to post-processing

## License

This project is open source. See the repository for license details.

## Support

For questions or issues, please create an issue on GitHub.