using JQLBridge.Core.Domain;

namespace JQLBridge.Core.QueryEngine;

public interface IQueryHandlerRegistry
{
    void RegisterHandler(IQueryHandler handler);
    IEnumerable<IQueryHandler> GetHandlers(QueryIntent intent);
    IEnumerable<IQueryHandler> GetAllHandlers();
}

public class QueryHandlerRegistry : IQueryHandlerRegistry
{
    private readonly List<IQueryHandler> _handlers = new();

    public void RegisterHandler(IQueryHandler handler)
    {
        _handlers.Add(handler);
    }

    public IEnumerable<IQueryHandler> GetHandlers(QueryIntent intent)
    {
        return _handlers.Where(h => h.CanHandle(intent));
    }

    public IEnumerable<IQueryHandler> GetAllHandlers()
    {
        return _handlers.ToList();
    }
}