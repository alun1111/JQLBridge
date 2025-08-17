using JQLBridge.Core.Domain;
using System.Text.RegularExpressions;

namespace JQLBridge.Core.Llm;

public class MockLlmClient : ILlmClient
{
    private static readonly Dictionary<string, QueryIntent> PresetResponses = new()
    {
        ["bugs assigned to me"] = new QueryIntent(
            Filters: new QueryFilters(
                Assignee: "currentUser",
                IssueTypes: new[] { "Bug" })),

        ["show bugs assigned to me updated last 7 days"] = new QueryIntent(
            Filters: new QueryFilters(
                Assignee: "currentUser",
                IssueTypes: new[] { "Bug" },
                Updated: new DateRange(LastDays: 7))),

        ["high priority stories in bank project"] = new QueryIntent(
            Filters: new QueryFilters(
                Project: "BANK",
                Priorities: new[] { "High" },
                IssueTypes: new[] { "Story" })),

        ["open issues"] = new QueryIntent(
            Filters: new QueryFilters(
                Status: new[] { "Open", "To Do" })),

        ["closed issues last month"] = new QueryIntent(
            Filters: new QueryFilters(
                Status: new[] { "Closed", "Done" },
                Updated: new DateRange(LastDays: 30)))
    };

    public Task<QueryIntent> ParseNaturalLanguageAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return ParseNaturalLanguageAsync(prompt, false, cancellationToken);
    }

    public Task<QueryIntent> ParseNaturalLanguageAsync(string prompt, bool debug, CancellationToken cancellationToken = default)
    {
        var normalizedPrompt = prompt.ToLowerInvariant().Trim();
        
        if (debug)
        {
            Console.WriteLine($"   📝 Mock LLM processing: '{prompt}'");
            Console.WriteLine($"   🔍 Normalized prompt: '{normalizedPrompt}'");
        }
        
        // Try exact matches first
        foreach (var preset in PresetResponses)
        {
            if (normalizedPrompt.Contains(preset.Key))
            {
                if (debug) Console.WriteLine($"   ✅ Found preset match for: '{preset.Key}'");
                return Task.FromResult(preset.Value);
            }
        }

        if (debug) Console.WriteLine($"   🤖 Using pattern matching fallback");
        
        // Fallback pattern matching
        var intent = ParseWithPatterns(normalizedPrompt);
        
        if (debug) Console.WriteLine($"   📤 Generated intent: Project={intent.Filters?.Project}, Status={intent.Filters?.Status?.Count}, IssueTypes={intent.Filters?.IssueTypes?.Count}");
        
        return Task.FromResult(intent);
    }

    private static QueryIntent ParseWithPatterns(string prompt)
    {
        var filters = new QueryFilters();
        var sort = new List<SortField>();
        int? limit = null;
        string? search = null;

        // Extract assignee
        if (prompt.Contains("assigned to me") || prompt.Contains("my "))
        {
            filters = filters with { Assignee = "currentUser" };
        }
        else if (Regex.IsMatch(prompt, @"assigned to (\w+)"))
        {
            var match = Regex.Match(prompt, @"assigned to (\w+)");
            filters = filters with { Assignee = match.Groups[1].Value };
        }

        // Extract issue types
        var issueTypes = new List<string>();
        if (prompt.Contains("bug")) issueTypes.Add("Bug");
        if (prompt.Contains("story") || prompt.Contains("stories")) issueTypes.Add("Story");
        if (prompt.Contains("task")) issueTypes.Add("Task");
        if (prompt.Contains("epic")) issueTypes.Add("Epic");
        if (issueTypes.Any())
        {
            filters = filters with { IssueTypes = issueTypes };
        }

        // Extract status
        var statuses = new List<string>();
        if (prompt.Contains("open")) statuses.Add("Open");
        if (prompt.Contains("closed") || prompt.Contains("done")) statuses.AddRange(new[] { "Closed", "Done" });
        if (prompt.Contains("in progress")) statuses.Add("In Progress");
        if (statuses.Any())
        {
            filters = filters with { Status = statuses };
        }

        // Extract priorities
        var priorities = new List<string>();
        if (prompt.Contains("high priority")) priorities.Add("High");
        if (prompt.Contains("low priority")) priorities.Add("Low");
        if (prompt.Contains("medium priority")) priorities.Add("Medium");
        if (priorities.Any())
        {
            filters = filters with { Priorities = priorities };
        }

        // Extract project
        var projectMatch = Regex.Match(prompt, @"project (\w+)|in (\w+) project");
        if (projectMatch.Success)
        {
            var project = projectMatch.Groups[1].Value != "" ? projectMatch.Groups[1].Value : projectMatch.Groups[2].Value;
            filters = filters with { Project = project.ToUpperInvariant() };
        }

        // Extract date ranges
        if (prompt.Contains("last week") || prompt.Contains("past week"))
        {
            filters = filters with { Updated = new DateRange(LastDays: 7) };
        }
        else if (prompt.Contains("last month") || prompt.Contains("past month"))
        {
            filters = filters with { Updated = new DateRange(LastDays: 30) };
        }
        else if (Regex.IsMatch(prompt, @"last (\d+) days?"))
        {
            var match = Regex.Match(prompt, @"last (\d+) days?");
            if (int.TryParse(match.Groups[1].Value, out var days))
            {
                filters = filters with { Updated = new DateRange(LastDays: days) };
            }
        }

        // Extract limit
        var limitMatch = Regex.Match(prompt, @"(?:show|limit|top) (\d+)");
        if (limitMatch.Success && int.TryParse(limitMatch.Groups[1].Value, out var parsedLimit))
        {
            limit = parsedLimit;
        }

        // Extract sorting
        if (prompt.Contains("sorted by updated") || prompt.Contains("recent"))
        {
            sort.Add(new SortField("updated", SortOrder.Desc));
        }

        return new QueryIntent(
            Filters: filters,
            Search: search,
            Sort: sort.Any() ? sort : null,
            Limit: limit);
    }
}