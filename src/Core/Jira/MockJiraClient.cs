using JQLBridge.Core.Domain;
using JQLBridge.Core.QueryEngine;

namespace JQLBridge.Core.Jira;

public class MockJiraClient : IJiraClient
{
    private static readonly List<Issue> MockIssues = new()
    {
        new Issue(
            Id: "1",
            Key: "BANK-123",
            Summary: "Payment processing bug in checkout flow",
            Description: "Users unable to complete payment in checkout",
            Status: IssueStatus.Open,
            Priority: IssuePriority.High,
            Type: IssueType.Bug,
            Assignee: new User("john.doe", "John Doe", "john.doe@example.com"),
            Reporter: new User("jane.smith", "Jane Smith", "jane.smith@example.com"),
            Project: "BANK",
            Created: DateTime.Now.AddDays(-5),
            Updated: DateTime.Now.AddDays(-1),
            ResolutionDate: null,
            Labels: new[] { "payment", "urgent" },
            Components: new[] { "checkout" },
            FixVersions: new[] { "v2.1.0" },
            Resolution: null),

        new Issue(
            Id: "2",
            Key: "BANK-124",
            Summary: "Implement new user dashboard",
            Description: "Create a new dashboard for user analytics",
            Status: IssueStatus.InProgress,
            Priority: IssuePriority.Medium,
            Type: IssueType.Story,
            Assignee: new User("alice.jones", "Alice Jones", "alice.jones@example.com"),
            Reporter: new User("bob.wilson", "Bob Wilson", "bob.wilson@example.com"),
            Project: "BANK",
            Created: DateTime.Now.AddDays(-10),
            Updated: DateTime.Now.AddDays(-2),
            ResolutionDate: null,
            Labels: new[] { "dashboard", "analytics" },
            Components: new[] { "frontend" },
            FixVersions: new[] { "v2.2.0" },
            Resolution: null),

        new Issue(
            Id: "3",
            Key: "PROJ-456",
            Summary: "Database migration task",
            Description: "Migrate legacy data to new schema",
            Status: IssueStatus.Done,
            Priority: IssuePriority.Low,
            Type: IssueType.Task,
            Assignee: new User("john.doe", "John Doe", "john.doe@example.com"),
            Reporter: new User("admin", "System Admin", "admin@example.com"),
            Project: "PROJ",
            Created: DateTime.Now.AddDays(-30),
            Updated: DateTime.Now.AddDays(-7),
            ResolutionDate: DateTime.Now.AddDays(-7),
            Labels: new[] { "migration", "database" },
            Components: new[] { "backend" },
            FixVersions: new[] { "v1.5.0" },
            Resolution: "Fixed"),

        new Issue(
            Id: "4",
            Key: "BANK-125",
            Summary: "Fix login timeout issue",
            Description: "Users getting logged out too quickly",
            Status: IssueStatus.Open,
            Priority: IssuePriority.Medium,
            Type: IssueType.Bug,
            Assignee: null,
            Reporter: new User("jane.smith", "Jane Smith", "jane.smith@example.com"),
            Project: "BANK",
            Created: DateTime.Now.AddDays(-3),
            Updated: DateTime.Now.AddHours(-6),
            ResolutionDate: null,
            Labels: new[] { "security", "auth" },
            Components: new[] { "authentication" },
            FixVersions: new[] { "v2.0.1" },
            Resolution: null)
    };

    public Task<QueryResult> SearchAsync(JqlQuery query, CancellationToken cancellationToken = default)
    {
        var filteredIssues = ApplyJqlFilter(query.Query);
        
        var issues = filteredIssues.Take(query.MaxResults ?? 50).ToList();
        
        var result = new QueryResult(
            Issues: issues,
            GeneratedJql: query.Query,
            Total: filteredIssues.Count);

        return Task.FromResult(result);
    }

    public Task<Issue?> GetIssueAsync(string issueKey, CancellationToken cancellationToken = default)
    {
        var issue = MockIssues.FirstOrDefault(i => 
            i.Key.Equals(issueKey, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(issue);
    }

    public Task<IReadOnlyList<string>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        var projects = MockIssues.Select(i => i.Project).Distinct().ToList();
        return Task.FromResult<IReadOnlyList<string>>(projects);
    }

    private static IReadOnlyList<Issue> ApplyJqlFilter(string jql)
    {
        if (string.IsNullOrEmpty(jql))
            return MockIssues;

        var issues = MockIssues.AsEnumerable();
        var lowerJql = jql.ToLowerInvariant();

        // Simple JQL parsing for mock purposes
        if (lowerJql.Contains("assignee = currentuser()"))
        {
            issues = issues.Where(i => i.Assignee?.Id == "john.doe");
        }
        
        if (lowerJql.Contains("assignee is empty"))
        {
            issues = issues.Where(i => i.Assignee == null);
        }

        if (lowerJql.Contains("type = \"bug\""))
        {
            issues = issues.Where(i => i.Type == IssueType.Bug);
        }

        if (lowerJql.Contains("type = \"story\""))
        {
            issues = issues.Where(i => i.Type == IssueType.Story);
        }

        if (lowerJql.Contains("project = \"bank\""))
        {
            issues = issues.Where(i => i.Project == "BANK");
        }

        if (lowerJql.Contains("status = \"open\""))
        {
            issues = issues.Where(i => i.Status == IssueStatus.Open);
        }

        if (lowerJql.Contains("updated >= -7d"))
        {
            var cutoff = DateTime.Now.AddDays(-7);
            issues = issues.Where(i => i.Updated >= cutoff);
        }

        if (lowerJql.Contains("priority = \"high\""))
        {
            issues = issues.Where(i => i.Priority == IssuePriority.High);
        }

        return issues.ToList();
    }
}