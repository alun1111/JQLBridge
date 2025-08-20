using JQLBridge.Core.Domain;
using JQLBridge.Core.Jql;

namespace JQLBridge.Core.Jira;

public interface IJiraClient
{
    Task<QueryResult> SearchAsync(JqlQuery query, CancellationToken cancellationToken = default);
    Task<Issue?> GetIssueAsync(string issueKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetProjectsAsync(CancellationToken cancellationToken = default);
}

public record JiraSearchRequest(
    string Jql,
    int? MaxResults = null,
    int? StartAt = null,
    IReadOnlyList<string>? Fields = null);

public record JiraSearchResponse(
    IReadOnlyList<Issue> Issues,
    int Total,
    int MaxResults,
    int StartAt);