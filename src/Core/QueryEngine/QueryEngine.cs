using JQLBridge.Core.Domain;

namespace JQLBridge.Core.QueryEngine;

public class QueryEngine
{
    private readonly IQueryHandlerRegistry _handlerRegistry;
    private readonly IJqlBuilder _jqlBuilder;

    public QueryEngine(IQueryHandlerRegistry handlerRegistry, IJqlBuilder jqlBuilder)
    {
        _handlerRegistry = handlerRegistry;
        _jqlBuilder = jqlBuilder;
    }

    public JqlQuery BuildQuery(QueryIntent intent)
    {
        var handlers = _handlerRegistry.GetHandlers(intent);
        
        var fragments = new List<JqlFragment>();
        
        foreach (var handler in handlers)
        {
            var fragment = handler.Handle(intent);
            if (fragment != null)
            {
                fragments.Add(fragment);
            }
        }

        var combinedFragment = CombineFragments(fragments);
        return _jqlBuilder.BuildQuery(combinedFragment, intent);
    }

    private static JqlFragment CombineFragments(IEnumerable<JqlFragment> fragments)
    {
        return fragments.Aggregate(new JqlFragment(), (acc, fragment) => acc.Combine(fragment));
    }
}

public record JqlQuery(
    string Query,
    int? MaxResults = null,
    int? StartAt = null);