using JQLBridge.Core.Domain;

namespace JQLBridge.Core.QueryEngine.Handlers;

public class IssueTypeHandler : IQueryHandler
{
    public string Name => "IssueType";

    public bool CanHandle(QueryIntent intent)
    {
        return intent.Filters?.IssueTypes?.Any() == true;
    }

    public JqlFragment Handle(QueryIntent intent)
    {
        var issueTypes = intent.Filters?.IssueTypes;
        if (issueTypes == null || !issueTypes.Any())
            return new JqlFragment();

        var typeList = string.Join(", ", issueTypes.Select(t => $"\"{t}\""));
        var whereClause = issueTypes.Count == 1 
            ? $"type = \"{issueTypes.First()}\""
            : $"type IN ({typeList})";

        return new JqlFragment(WhereClause: whereClause);
    }
}