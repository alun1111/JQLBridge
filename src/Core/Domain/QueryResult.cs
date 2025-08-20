namespace JQLBridge.Core.Domain;

public record QueryResult(
    IReadOnlyList<Issue> Issues,
    string GeneratedJql,
    int Total,
    IReadOnlyDictionary<string, object>? Aggregations = null,
    ProcessedData? ProcessedData = null);

public record ProcessedData(
    IReadOnlyList<DataGroup> Groups,
    IReadOnlyDictionary<string, object> Calculations,
    IReadOnlyDictionary<string, object> Metadata);

public record DataGroup(
    string Key,
    object Value,
    IReadOnlyList<Issue> Issues,
    IReadOnlyList<DataGroup>? SubGroups = null,
    IReadOnlyDictionary<string, object>? Aggregations = null);

