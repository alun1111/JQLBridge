using JQLBridge.Core.Domain;

namespace JQLBridge.Core.QueryEngine.Handlers;

public class DateRangeHandler : IQueryHandler
{
    public string Name => "DateRange";

    public bool CanHandle(QueryIntent intent)
    {
        return intent.Filters?.Updated != null || intent.Filters?.Created != null;
    }

    public JqlFragment Handle(QueryIntent intent)
    {
        var whereClauses = new List<string>();

        if (intent.Filters?.Updated != null)
        {
            var updatedClause = BuildDateClause("updated", intent.Filters.Updated);
            if (!string.IsNullOrEmpty(updatedClause))
                whereClauses.Add(updatedClause);
        }

        if (intent.Filters?.Created != null)
        {
            var createdClause = BuildDateClause("created", intent.Filters.Created);
            if (!string.IsNullOrEmpty(createdClause))
                whereClauses.Add(createdClause);
        }

        if (!whereClauses.Any())
            return new JqlFragment();

        var whereClause = string.Join(" AND ", whereClauses);
        return new JqlFragment(WhereClause: whereClause);
    }

    private static string? BuildDateClause(string field, DateRange dateRange)
    {
        if (dateRange.LastDays.HasValue)
        {
            return $"{field} >= -{dateRange.LastDays}d";
        }

        var clauses = new List<string>();

        if (dateRange.From.HasValue)
        {
            var fromDate = dateRange.From.Value.ToString("yyyy-MM-dd");
            clauses.Add($"{field} >= \"{fromDate}\"");
        }

        if (dateRange.To.HasValue)
        {
            var toDate = dateRange.To.Value.ToString("yyyy-MM-dd");
            clauses.Add($"{field} <= \"{toDate}\"");
        }

        return clauses.Any() ? string.Join(" AND ", clauses) : null;
    }
}