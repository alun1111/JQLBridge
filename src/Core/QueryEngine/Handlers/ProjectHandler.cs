using JQLBridge.Core.Domain;

namespace JQLBridge.Core.QueryEngine.Handlers;

public class ProjectHandler : IQueryHandler
{
    public string Name => "Project";

    public bool CanHandle(QueryIntent intent)
    {
        return !string.IsNullOrEmpty(intent.Filters?.Project);
    }

    public JqlFragment Handle(QueryIntent intent)
    {
        var project = intent.Filters?.Project;
        if (string.IsNullOrEmpty(project))
            return new JqlFragment();

        var whereClause = $"project = \"{project}\"";
        return new JqlFragment(WhereClause: whereClause);
    }
}