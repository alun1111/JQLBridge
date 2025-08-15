namespace JQLBridge.Core.Domain;

public record QueryResult(
    IReadOnlyList<Issue> Issues,
    string GeneratedJql,
    int Total,
    IReadOnlyDictionary<string, object>? Aggregations = null);

public record AggregationResult(
    AggregationType Type,
    object Value);