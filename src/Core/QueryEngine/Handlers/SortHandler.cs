using JQLBridge.Core.Domain;

namespace JQLBridge.Core.QueryEngine.Handlers;

public class SortHandler : IQueryHandler
{
    public string Name => "Sort";

    public bool CanHandle(QueryIntent intent)
    {
        return intent.Sort?.Any() == true;
    }

    public JqlFragment Handle(QueryIntent intent)
    {
        var sortFields = intent.Sort;
        if (sortFields == null || !sortFields.Any())
            return new JqlFragment();

        var orderByParts = sortFields.Select(sf => 
            $"{sf.Field} {(sf.Order == SortOrder.Asc ? "ASC" : "DESC")}");
        
        var orderByClause = string.Join(", ", orderByParts);
        
        return new JqlFragment(OrderByClause: orderByClause);
    }
}