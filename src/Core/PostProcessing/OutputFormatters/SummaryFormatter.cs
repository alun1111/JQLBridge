using JQLBridge.Core.Domain;

namespace JQLBridge.Core.PostProcessing.OutputFormatters;

public class SummaryFormatter : IOutputFormatter
{
    public string Format => "summary";

    public bool CanFormat(string format) => format.Equals("summary", StringComparison.OrdinalIgnoreCase);

    public Task<string> FormatAsync(QueryResult result, ProcessingResult? processed = null)
    {
        var output = new List<string>();
        
        output.Add($"Query Summary: {result.Total} issues found");
        output.Add($"JQL: {result.GeneratedJql}");
        output.Add("");

        if (processed?.Groups.Any() == true)
        {
            output.Add("Grouping Summary:");
            foreach (var group in processed.Groups)
            {
                output.Add($"  {group.Key} '{group.Value}': {group.Issues.Count} issues");
                
                if (group.Aggregations?.Any() == true)
                {
                    foreach (var agg in group.Aggregations)
                    {
                        output.Add($"    {agg.Key}: {agg.Value}");
                    }
                }
            }
            output.Add("");
        }

        if (result.Issues.Any())
        {
            var statusCounts = result.Issues.GroupBy(i => i.Status)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());
            
            output.Add("Status Distribution:");
            foreach (var status in statusCounts.OrderByDescending(s => s.Value))
            {
                output.Add($"  {status.Key}: {status.Value} ({status.Value * 100.0 / result.Issues.Count:F1}%)");
            }
            output.Add("");

            var assigneeCounts = result.Issues.GroupBy(i => i.Assignee?.DisplayName ?? "Unassigned")
                .ToDictionary(g => g.Key, g => g.Count());
            
            output.Add("Top Assignees:");
            foreach (var assignee in assigneeCounts.OrderByDescending(a => a.Value).Take(5))
            {
                output.Add($"  {assignee.Key}: {assignee.Value} issues");
            }
            output.Add("");

            var avgAge = result.Issues.Select(i => (DateTime.UtcNow - i.Created).TotalDays).Average();
            var avgDaysSinceUpdate = result.Issues.Select(i => (DateTime.UtcNow - i.Updated).TotalDays).Average();
            
            output.Add("Time Metrics:");
            output.Add($"  Average age: {avgAge:F1} days");
            output.Add($"  Average days since update: {avgDaysSinceUpdate:F1} days");
        }

        if (processed?.Calculations.Any() == true)
        {
            output.Add("");
            output.Add("Custom Calculations:");
            foreach (var calc in processed.Calculations)
            {
                output.Add($"  {calc.Key}: {calc.Value}");
            }
        }

        if (processed?.Aggregations.Any() == true)
        {
            output.Add("");
            output.Add("Custom Aggregations:");
            foreach (var agg in processed.Aggregations)
            {
                output.Add($"  {agg.Key}: {agg.Value}");
            }
        }

        return Task.FromResult(string.Join(Environment.NewLine, output));
    }
}