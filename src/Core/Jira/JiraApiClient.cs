using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JQLBridge.Core.Domain;
using JQLBridge.Core.Jql;
using Microsoft.Extensions.Logging;

namespace JQLBridge.Core.Jira;

public class JiraApiClient : IJiraClient
{
    private readonly HttpClient _httpClient;
    private readonly JiraConfiguration _configuration;
    private readonly ILogger<JiraApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public JiraApiClient(HttpClient httpClient, JiraConfiguration configuration, ILogger<JiraApiClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        if (!string.IsNullOrEmpty(_configuration.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_configuration.BaseUrl.TrimEnd('/'));
        }

        if (!string.IsNullOrEmpty(_configuration.Email) && !string.IsNullOrEmpty(_configuration.ApiToken))
        {
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_configuration.Email}:{_configuration.ApiToken}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        }

        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "JQLBridge/1.0");
    }

    public async Task<QueryResult> SearchAsync(JqlQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new JiraApiSearchRequest(
                jql: query.Query,
                maxResults: query.MaxResults ?? 50,
                startAt: 0,
                fields: new[] { "id", "key", "summary", "description", "status", "priority", "issuetype", 
                               "assignee", "reporter", "project", "created", "updated", "resolutiondate", 
                               "labels", "components", "fixVersions", "resolution" }
            );

            var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            _logger.LogDebug("Sending JQL search request: {Jql}", query.Query);
            
            var response = await _httpClient.PostAsync("/rest/api/3/search", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var searchResponse = JsonSerializer.Deserialize<JiraApiSearchResponse>(responseContent, _jsonOptions);

            if (searchResponse == null)
                throw new InvalidOperationException("Failed to deserialize Jira search response");

            var issues = searchResponse.Issues.Select(MapToIssue).ToList();

            return new QueryResult(
                Issues: issues,
                GeneratedJql: query.Query,
                Total: searchResponse.Total);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while searching Jira issues");
            throw new JiraApiException("Failed to search Jira issues due to network error", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Jira API response");
            throw new JiraApiException("Failed to parse Jira API response", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while searching Jira issues");
            throw new JiraApiException("Unexpected error occurred while searching Jira issues", ex);
        }
    }

    public async Task<Issue?> GetIssueAsync(string issueKey, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching issue: {IssueKey}", issueKey);
            
            var response = await _httpClient.GetAsync($"/rest/api/3/issue/{issueKey}", cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
                
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiIssue = JsonSerializer.Deserialize<JiraApiIssue>(responseContent, _jsonOptions);

            return apiIssue != null ? MapToIssue(apiIssue) : null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while fetching issue {IssueKey}", issueKey);
            throw new JiraApiException($"Failed to fetch issue {issueKey} due to network error", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Jira API response for issue {IssueKey}", issueKey);
            throw new JiraApiException($"Failed to parse Jira API response for issue {issueKey}", ex);
        }
    }

    public async Task<IReadOnlyList<string>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching projects");
            
            var response = await _httpClient.GetAsync("/rest/api/3/project", cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var projects = JsonSerializer.Deserialize<JiraApiProject[]>(responseContent, _jsonOptions);

            return projects?.Select(p => p.Key).ToList() ?? new List<string>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while fetching projects");
            throw new JiraApiException("Failed to fetch projects due to network error", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Jira projects API response");
            throw new JiraApiException("Failed to parse Jira projects API response", ex);
        }
    }

    private static Issue MapToIssue(JiraApiIssue apiIssue)
    {
        return new Issue(
            Id: apiIssue.Id,
            Key: apiIssue.Key,
            Summary: apiIssue.Fields.Summary ?? string.Empty,
            Description: apiIssue.Fields.Description ?? string.Empty,
            Status: MapStatus(apiIssue.Fields.Status.Name),
            Priority: MapPriority(apiIssue.Fields.Priority.Name),
            Type: MapIssueType(apiIssue.Fields.IssueType.Name),
            Assignee: apiIssue.Fields.Assignee != null ? MapUser(apiIssue.Fields.Assignee) : null,
            Reporter: MapUser(apiIssue.Fields.Reporter),
            Project: apiIssue.Fields.Project.Key,
            Created: DateTime.Parse(apiIssue.Fields.Created),
            Updated: DateTime.Parse(apiIssue.Fields.Updated),
            ResolutionDate: !string.IsNullOrEmpty(apiIssue.Fields.ResolutionDate) ? DateTime.Parse(apiIssue.Fields.ResolutionDate) : null,
            Labels: apiIssue.Fields.Labels ?? new List<string>(),
            Components: apiIssue.Fields.Components?.Select(c => c.Name).ToList() ?? new List<string>(),
            FixVersions: apiIssue.Fields.FixVersions?.Select(v => v.Name).ToList() ?? new List<string>(),
            Resolution: apiIssue.Fields.Resolution?.Name
        );
    }

    private static User MapUser(JiraApiUser apiUser)
    {
        return new User(
            Id: apiUser.AccountId ?? apiUser.Name ?? string.Empty,
            DisplayName: apiUser.DisplayName ?? string.Empty,
            EmailAddress: apiUser.EmailAddress ?? string.Empty
        );
    }

    private static IssueStatus MapStatus(string statusName)
    {
        return statusName.ToLowerInvariant() switch
        {
            "open" => IssueStatus.Open,
            "in progress" => IssueStatus.InProgress,
            "resolved" => IssueStatus.Resolved,
            "closed" => IssueStatus.Closed,
            "to do" => IssueStatus.ToDo,
            "done" => IssueStatus.Done,
            "blocked" => IssueStatus.Blocked,
            "in review" => IssueStatus.InReview,
            _ => IssueStatus.Open
        };
    }

    private static IssuePriority MapPriority(string priorityName)
    {
        return priorityName.ToLowerInvariant() switch
        {
            "lowest" => IssuePriority.Lowest,
            "low" => IssuePriority.Low,
            "medium" => IssuePriority.Medium,
            "high" => IssuePriority.High,
            "highest" => IssuePriority.Highest,
            _ => IssuePriority.Medium
        };
    }

    private static IssueType MapIssueType(string typeName)
    {
        return typeName.ToLowerInvariant() switch
        {
            "bug" => IssueType.Bug,
            "story" => IssueType.Story,
            "task" => IssueType.Task,
            "epic" => IssueType.Epic,
            "sub-task" => IssueType.SubTask,
            "subtask" => IssueType.SubTask,
            _ => IssueType.Task
        };
    }
}

public record JiraConfiguration(
    string? BaseUrl,
    string? Email,
    string? ApiToken);

public class JiraApiException : Exception
{
    public JiraApiException(string message) : base(message) { }
    public JiraApiException(string message, Exception innerException) : base(message, innerException) { }
}

// API DTOs
internal record JiraApiSearchRequest(
    string jql,
    int maxResults,
    int startAt,
    IReadOnlyList<string> fields);

internal record JiraApiSearchResponse(
    IReadOnlyList<JiraApiIssue> Issues,
    int Total,
    int MaxResults,
    int StartAt);

internal record JiraApiIssue(
    string Id,
    string Key,
    JiraApiIssueFields Fields);

internal record JiraApiIssueFields(
    string? Summary,
    string? Description,
    JiraApiStatus Status,
    JiraApiPriority Priority,
    JiraApiIssueType IssueType,
    JiraApiUser? Assignee,
    JiraApiUser Reporter,
    JiraApiProject Project,
    string Created,
    string Updated,
    string? ResolutionDate,
    IReadOnlyList<string>? Labels,
    IReadOnlyList<JiraApiComponent>? Components,
    IReadOnlyList<JiraApiVersion>? FixVersions,
    JiraApiResolution? Resolution);

internal record JiraApiStatus(string Name);
internal record JiraApiPriority(string Name);
internal record JiraApiIssueType(string Name);
internal record JiraApiProject(string Key, string Name);
internal record JiraApiComponent(string Name);
internal record JiraApiVersion(string Name);
internal record JiraApiResolution(string Name);

internal record JiraApiUser(
    string? AccountId,
    string? Name,
    string? DisplayName,
    string? EmailAddress);