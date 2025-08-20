namespace JQLBridge.Core.Domain;

public record QueryIntent(
    QueryFilters? Filters = null,
    string? Search = null,
    IReadOnlyList<SortField>? Sort = null,
    int? Limit = null);

public record QueryFilters(
    string? Project = null,
    string? Assignee = null,
    IReadOnlyList<string>? Status = null,
    DateRange? Updated = null,
    DateRange? Created = null,
    IReadOnlyList<string>? Labels = null,
    IReadOnlyList<string>? Components = null,
    IReadOnlyList<string>? IssueTypes = null,
    IReadOnlyList<string>? Priorities = null);

public record DateRange(
    DateTime? From = null,
    DateTime? To = null,
    int? LastDays = null);

public record SortField(
    string Field,
    SortOrder Order = SortOrder.Asc);

public enum SortOrder
{
    Asc,
    Desc
}

