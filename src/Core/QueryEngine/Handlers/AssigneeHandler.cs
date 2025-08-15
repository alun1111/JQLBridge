using JQLBridge.Core.Domain;

namespace JQLBridge.Core.QueryEngine.Handlers;

public class AssigneeHandler : IQueryHandler
{
    public string Name => "Assignee";

    public bool CanHandle(QueryIntent intent)
    {
        return !string.IsNullOrEmpty(intent.Filters?.Assignee);
    }

    public JqlFragment Handle(QueryIntent intent)
    {
        var assignee = intent.Filters?.Assignee;
        if (string.IsNullOrEmpty(assignee))
            return new JqlFragment();

        var whereClause = assignee.ToLowerInvariant() switch
        {
            "currentuser" => "assignee = currentUser()",
            "unassigned" => "assignee is EMPTY",
            _ => $"assignee = \"{assignee}\""
        };

        return new JqlFragment(WhereClause: whereClause);
    }
}