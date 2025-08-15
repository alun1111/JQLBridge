using JQLBridge.Core.Domain;

namespace JQLBridge.Core.QueryEngine;

public interface IJqlBuilder
{
    JqlQuery BuildQuery(JqlFragment fragment, QueryIntent intent);
}

public class JqlBuilder : IJqlBuilder
{
    public JqlQuery BuildQuery(JqlFragment fragment, QueryIntent intent)
    {
        var query = BuildQueryString(fragment);
        var maxResults = intent.Limit;
        
        return new JqlQuery(query, maxResults);
    }

    private static string BuildQueryString(JqlFragment fragment)
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrEmpty(fragment.WhereClause))
        {
            parts.Add(fragment.WhereClause);
        }
        
        if (!string.IsNullOrEmpty(fragment.OrderByClause))
        {
            parts.Add($"ORDER BY {fragment.OrderByClause}");
        }

        return string.Join(" ", parts);
    }
}