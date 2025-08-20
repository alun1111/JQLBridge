using JQLBridge.Core.Domain;
using JQLBridge.Core.Processing;
using System.Text.Json;
namespace JQLBridge.Core.Output;

public interface IOutputFormatter
{
    string Name { get; }
    Task<string> FormatAsync(QueryResult result, ProcessingResult? processed = null);
}

public class TableFormatter : IOutputFormatter
{
    public string Name => "table";

    public Task<string> FormatAsync(QueryResult result, ProcessingResult? processed = null)
    {
        if (!result.Issues.Any())
        {
            return Task.FromResult("No issues found.");
        }

        var output = new List<string>();
        
        // Simple text table format
        output.Add("┌──────────┬─────────────────────────────────┬────────────┬──────────────┬────────────┐");
        output.Add("│ Key      │ Summary                         │ Status     │ Assignee     │ Updated    │");
        output.Add("├──────────┼─────────────────────────────────┼────────────┼──────────────┼────────────┤");

        foreach (var issue in result.Issues)
        {
            var summary = issue.Summary.Length > 31 ? issue.Summary[..28] + "..." : issue.Summary;
            var status = issue.Status.ToString();
            status = status.Length > 10 ? status[..7] + "..." : status;
            var assignee = issue.Assignee?.DisplayName ?? "Unassigned";
            assignee = assignee.Length > 12 ? assignee[..9] + "..." : assignee;
            
            output.Add($"│ {issue.Key,-8} │ {summary,-31} │ {status,-10} │ {assignee,-12} │ {issue.Updated:yyyy-MM-dd} │");
        }
        
        output.Add("└──────────┴─────────────────────────────────┴────────────┴──────────────┴────────────┘");
        
        return Task.FromResult(string.Join(Environment.NewLine, output));
    }
}

public class JsonFormatter : IOutputFormatter
{
    public string Name => "json";

    public Task<string> FormatAsync(QueryResult result, ProcessingResult? processed = null)
    {
        var output = new
        {
            GeneratedJql = result.GeneratedJql,
            Total = result.Total,
            Issues = result.Issues.Select(i => new
            {
                i.Key,
                i.Summary,
                i.Status,
                i.Assignee,
                i.Priority,
                IssueType = i.Type.ToString(),
                Created = i.Created.ToString("yyyy-MM-dd"),
                Updated = i.Updated.ToString("yyyy-MM-dd")
            }),
            ProcessedData = processed != null ? new
            {
                Groups = processed.Groups.Count,
                Calculations = processed.Calculations,
                Aggregations = processed.Aggregations
            } : null
        };

        return Task.FromResult(JsonSerializer.Serialize(output, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }
}

public class SummaryFormatter : IOutputFormatter
{
    public string Name => "summary";

    public Task<string> FormatAsync(QueryResult result, ProcessingResult? processed = null)
    {
        var summary = new List<string>
        {
            $"JQL Query: {result.GeneratedJql}",
            $"Total Issues: {result.Total}",
            $"Returned: {result.Issues.Count}",
            ""
        };

        if (result.Issues.Any())
        {
            var statusCounts = result.Issues
                .GroupBy(i => i.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            summary.Add("Status Distribution:");
            foreach (var status in statusCounts)
            {
                summary.Add($"  {status.Key}: {status.Value}");
            }
            summary.Add("");

            var assigneeCounts = result.Issues
                .GroupBy(i => i.Assignee?.DisplayName ?? "Unassigned")
                .ToDictionary(g => g.Key, g => g.Count());

            summary.Add("Assignee Distribution:");
            foreach (var assignee in assigneeCounts.Take(5))
            {
                summary.Add($"  {assignee.Key}: {assignee.Value}");
            }
        }

        if (processed?.Calculations.Any() == true)
        {
            summary.Add("");
            summary.Add("Calculations:");
            foreach (var calc in processed.Calculations)
            {
                summary.Add($"  {calc.Key}: {calc.Value}");
            }
        }

        return Task.FromResult(string.Join(Environment.NewLine, summary));
    }
}

public class OutputFormatterRegistry
{
    private readonly Dictionary<string, IOutputFormatter> _formatters = new();

    public void RegisterFormatter(IOutputFormatter formatter)
    {
        _formatters[formatter.Name] = formatter;
    }

    public IOutputFormatter? GetFormatter(string name)
    {
        return _formatters.TryGetValue(name, out var formatter) ? formatter : null;
    }
}