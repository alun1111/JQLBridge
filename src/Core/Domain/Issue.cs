namespace JQLBridge.Core.Domain;

public record Issue(
    string Id,
    string Key,
    string Summary,
    string Description,
    IssueStatus Status,
    IssuePriority Priority,
    IssueType Type,
    User? Assignee,
    User Reporter,
    string Project,
    DateTime Created,
    DateTime Updated,
    DateTime? ResolutionDate,
    IReadOnlyList<string> Labels,
    IReadOnlyList<string> Components,
    IReadOnlyList<string> FixVersions,
    string? Resolution);

public record User(
    string Id,
    string DisplayName,
    string EmailAddress);

public enum IssueStatus
{
    Open,
    InProgress,
    Resolved,
    Closed,
    ToDo,
    Done,
    Blocked,
    InReview
}

public enum IssuePriority
{
    Lowest,
    Low,
    Medium,
    High,
    Highest
}

public enum IssueType
{
    Bug,
    Story,
    Task,
    Epic,
    SubTask
}