using JQLBridge.Core.Domain;

namespace JQLBridge.Core.QueryEngine.Handlers;

public class StatusHandler : IQueryHandler
{
    public string Name => "Status";

    public bool CanHandle(QueryIntent intent)
    {
        return intent.Filters?.Status?.Any() == true;
    }

    public JqlFragment Handle(QueryIntent intent)
    {
        var statuses = intent.Filters?.Status;
        if (statuses == null || !statuses.Any())
            return new JqlFragment();

        var statusList = string.Join(", ", statuses.Select(s => $"\"{s}\""));
        var whereClause = statuses.Count == 1 
            ? $"status = \"{statuses.First()}\""
            : $"status IN ({statusList})";

        return new JqlFragment(WhereClause: whereClause);
    }
}