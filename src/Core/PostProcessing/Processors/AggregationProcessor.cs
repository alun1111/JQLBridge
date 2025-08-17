using JQLBridge.Core.Domain;

namespace JQLBridge.Core.PostProcessing.Processors;

public class AggregationProcessor : IDataProcessor
{
    private readonly IAggregationEngine _aggregationEngine;
    
    public int Order => 30;

    public AggregationProcessor(IAggregationEngine aggregationEngine)
    {
        _aggregationEngine = aggregationEngine;
    }

    public bool CanProcess(ProcessingContext context)
    {
        return context.Options.Aggregate?.Any() == true;
    }

    public Task<ProcessingResult> ProcessAsync(ProcessingContext context)
    {
        if (!CanProcess(context))
        {
            return Task.FromResult(new ProcessingResult { Issues = context.Issues });
        }

        var aggregations = _aggregationEngine.Aggregate(context.Issues, context.Options.Aggregate!);
        
        return Task.FromResult(new ProcessingResult
        {
            Issues = context.Issues,
            Aggregations = aggregations
        });
    }
}

public interface IAggregationEngine
{
    IReadOnlyDictionary<string, object> Aggregate(IEnumerable<Issue> issues, IReadOnlyList<string> aggregations);
    IReadOnlyDictionary<string, object> AggregateGroups(IEnumerable<DataGroup> groups, IReadOnlyList<string> aggregations);
    void RegisterAggregation(string name, Func<IEnumerable<Issue>, object> aggregator);
}

public class AggregationEngine : IAggregationEngine
{
    private readonly Dictionary<string, Func<IEnumerable<Issue>, object>> _aggregations = new();

    public AggregationEngine()
    {
        RegisterDefaultAggregations();
    }

    public IReadOnlyDictionary<string, object> Aggregate(IEnumerable<Issue> issues, IReadOnlyList<string> aggregations)
    {
        var result = new Dictionary<string, object>();
        var issueList = issues.ToList();

        foreach (var aggregation in aggregations)
        {
            if (_aggregations.TryGetValue(aggregation.ToLowerInvariant(), out var aggregator))
            {
                result[aggregation] = aggregator(issueList);
            }
        }

        return result;
    }

    public IReadOnlyDictionary<string, object> AggregateGroups(IEnumerable<DataGroup> groups, IReadOnlyList<string> aggregations)
    {
        var result = new Dictionary<string, object>();
        
        foreach (var aggregation in aggregations)
        {
            if (_aggregations.TryGetValue(aggregation.ToLowerInvariant(), out var aggregator))
            {
                var groupResults = groups.ToDictionary(
                    g => g.Value.ToString() ?? "Unknown",
                    g => aggregator(g.Issues)
                );
                result[aggregation] = groupResults;
            }
        }

        return result;
    }

    public void RegisterAggregation(string name, Func<IEnumerable<Issue>, object> aggregator)
    {
        _aggregations[name.ToLowerInvariant()] = aggregator;
    }

    private void RegisterDefaultAggregations()
    {
        RegisterAggregation("count", issues => issues.Count());
        RegisterAggregation("avg_age", issues => 
            issues.Select(i => (DateTime.UtcNow - i.Created).TotalDays).DefaultIfEmpty(0).Average());
        RegisterAggregation("max_age", issues => 
            issues.Select(i => (DateTime.UtcNow - i.Created).TotalDays).DefaultIfEmpty(0).Max());
        RegisterAggregation("min_age", issues => 
            issues.Select(i => (DateTime.UtcNow - i.Created).TotalDays).DefaultIfEmpty(0).Min());
        RegisterAggregation("status_counts", issues => 
            issues.GroupBy(i => i.Status).ToDictionary(g => g.Key.ToString(), g => g.Count()));
        RegisterAggregation("priority_counts", issues => 
            issues.GroupBy(i => i.Priority).ToDictionary(g => g.Key.ToString(), g => g.Count()));
        RegisterAggregation("assignee_counts", issues => 
            issues.GroupBy(i => i.Assignee?.DisplayName ?? "Unassigned").ToDictionary(g => g.Key, g => g.Count()));
    }
}