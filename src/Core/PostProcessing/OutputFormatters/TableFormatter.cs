using JQLBridge.Core.Domain;

namespace JQLBridge.Core.PostProcessing.OutputFormatters;

public class TableFormatter : IOutputFormatter
{
    public string Format => "table";

    public bool CanFormat(string format) => format.Equals("table", StringComparison.OrdinalIgnoreCase);

    public Task<string> FormatAsync(QueryResult result, ProcessingResult? processed = null)
    {
        var output = new List<string>();
        
        output.Add($"Generated JQL: {result.GeneratedJql}");
        output.Add("");

        if (processed?.Groups.Any() == true)
        {
            output.AddRange(FormatGroups(processed.Groups));
        }
        else if (result.Issues.Any())
        {
            output.AddRange(FormatIssuesTable(result.Issues, result.Total));
        }
        else
        {
            output.Add("No issues found matching your query.");
        }

        if (processed?.Calculations.Any() == true)
        {
            output.Add("");
            output.Add("Calculations:");
            foreach (var calc in processed.Calculations)
            {
                output.Add($"  {calc.Key}: {calc.Value}");
            }
        }

        if (processed?.Aggregations.Any() == true)
        {
            output.Add("");
            output.Add("Aggregations:");
            foreach (var agg in processed.Aggregations)
            {
                output.Add($"  {agg.Key}: {agg.Value}");
            }
        }

        return Task.FromResult(string.Join(Environment.NewLine, output));
    }

    private static IEnumerable<string> FormatGroups(IReadOnlyList<DataGroup> groups, int indent = 0)
    {
        var indentStr = new string(' ', indent * 2);
        
        foreach (var group in groups)
        {
            yield return $"{indentStr}{group.Key}: {group.Value} ({group.Issues.Count} issues)";
            
            if (group.Aggregations?.Any() == true)
            {
                foreach (var agg in group.Aggregations)
                {
                    yield return $"{indentStr}  {agg.Key}: {agg.Value}";
                }
            }
            
            if (group.SubGroups?.Any() == true)
            {
                foreach (var line in FormatGroups(group.SubGroups, indent + 1))
                {
                    yield return line;
                }
            }
            else if (group.Issues.Any())
            {
                foreach (var issue in group.Issues.Take(5))
                {
                    yield return $"{indentStr}  - {issue.Key}: {TruncateText(issue.Summary, 40)}";
                }
                if (group.Issues.Count > 5)
                {
                    yield return $"{indentStr}  ... and {group.Issues.Count - 5} more";
                }
            }
            
            yield return "";
        }
    }

    private static IEnumerable<string> FormatIssuesTable(IReadOnlyList<Issue> issues, int total)
    {
        var lines = new List<string>();
        
        lines.Add("Key         | Summary                                      | Status      | Assignee         | Updated   ");
        lines.Add("------------|----------------------------------------------|-------------|------------------|----------");
        
        foreach (var issue in issues.Take(20))
        {
            var key = issue.Key.PadRight(11);
            var summary = TruncateText(issue.Summary, 44).PadRight(44);
            var status = issue.Status.ToString().PadRight(11);
            var assignee = TruncateText(issue.Assignee?.DisplayName ?? "Unassigned", 16).PadRight(16);
            var updated = issue.Updated.ToString("yyyy-MM-dd");
            
            lines.Add($"{key} | {summary} | {status} | {assignee} | {updated}");
        }
        
        if (issues.Count > 20)
        {
            lines.Add("");
            lines.Add($"Showing first 20 of {total} results");
        }
        
        return lines;
    }

    private static string TruncateText(string text, int maxLength)
    {
        return text.Length > maxLength ? text.Substring(0, maxLength - 3) + "..." : text;
    }
}