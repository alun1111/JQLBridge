using JQLBridge.Core.Domain;

namespace JQLBridge.Core.QueryEngine;

public interface IQueryHandler
{
    string Name { get; }
    bool CanHandle(QueryIntent intent);
    JqlFragment Handle(QueryIntent intent);
}

public record JqlFragment(
    string? WhereClause = null,
    string? OrderByClause = null,
    IReadOnlyDictionary<string, object>? Parameters = null);

public static class JqlFragmentExtensions
{
    public static JqlFragment Combine(this JqlFragment first, JqlFragment second)
    {
        var whereClause = CombineWhereClauses(first.WhereClause, second.WhereClause);
        var orderByClause = first.OrderByClause ?? second.OrderByClause;
        var parameters = CombineParameters(first.Parameters, second.Parameters);

        return new JqlFragment(whereClause, orderByClause, parameters);
    }

    private static string? CombineWhereClauses(string? first, string? second)
    {
        if (string.IsNullOrEmpty(first)) return second;
        if (string.IsNullOrEmpty(second)) return first;
        return $"({first}) AND ({second})";
    }

    private static IReadOnlyDictionary<string, object>? CombineParameters(
        IReadOnlyDictionary<string, object>? first, 
        IReadOnlyDictionary<string, object>? second)
    {
        if (first == null) return second;
        if (second == null) return first;

        var combined = new Dictionary<string, object>(first);
        foreach (var kvp in second)
        {
            combined[kvp.Key] = kvp.Value;
        }
        return combined;
    }
}