using System.Text.Json;
using JQLBridge.Core.Domain;

namespace JQLBridge.Core.PostProcessing.OutputFormatters;

public class JsonFormatter : IOutputFormatter
{
    public string Format => "json";

    public bool CanFormat(string format) => format.Equals("json", StringComparison.OrdinalIgnoreCase);

    public Task<string> FormatAsync(QueryResult result, ProcessingResult? processed = null)
    {
        var output = new
        {
            GeneratedJql = result.GeneratedJql,
            Total = result.Total,
            Issues = result.Issues.Select(FormatIssue),
            Groups = processed?.Groups?.Select(FormatGroup),
            Calculations = processed?.Calculations,
            Aggregations = processed?.Aggregations,
            LegacyAggregations = result.Aggregations
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return Task.FromResult(JsonSerializer.Serialize(output, options));
    }

    private static object FormatIssue(Issue issue)
    {
        return new
        {
            Key = issue.Key,
            Summary = issue.Summary,
            Status = issue.Status.ToString(),
            Priority = issue.Priority.ToString(),
            IssueType = issue.Type.ToString(),
            Project = issue.Project,
            Assignee = issue.Assignee != null ? new
            {
                DisplayName = issue.Assignee.DisplayName,
                EmailAddress = issue.Assignee.EmailAddress
            } : null,
            Reporter = issue.Reporter != null ? new
            {
                DisplayName = issue.Reporter.DisplayName,
                EmailAddress = issue.Reporter.EmailAddress
            } : null,
            Created = issue.Created,
            Updated = issue.Updated,
            Labels = issue.Labels,
            Components = issue.Components
        };
    }

    private static object FormatGroup(DataGroup group)
    {
        return new
        {
            Key = group.Key,
            Value = group.Value,
            IssueCount = group.Issues.Count,
            Issues = group.Issues.Select(FormatIssue),
            SubGroups = group.SubGroups?.Select(FormatGroup),
            Aggregations = group.Aggregations
        };
    }
}