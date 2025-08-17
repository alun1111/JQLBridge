using JQLBridge.Core.Domain;

namespace JQLBridge.Core.PostProcessing.Processors;

public class GroupingProcessor : IDataProcessor
{
    private readonly IGroupingEngine _groupingEngine;
    
    public int Order => 10;

    public GroupingProcessor(IGroupingEngine groupingEngine)
    {
        _groupingEngine = groupingEngine;
    }

    public bool CanProcess(ProcessingContext context)
    {
        return context.Options.GroupBy?.Any() == true;
    }

    public Task<ProcessingResult> ProcessAsync(ProcessingContext context)
    {
        if (!CanProcess(context))
        {
            return Task.FromResult(new ProcessingResult { Issues = context.Issues });
        }

        IDebugOutput debugOutput = context.Options.CustomOptions.ContainsKey("debug") && (bool)context.Options.CustomOptions["debug"] 
            ? new ConsoleDebugOutput(true) 
            : new NullDebugOutput();

        debugOutput.WriteData("Grouping by fields", context.Options.GroupBy!);
        
        var groups = _groupingEngine.GroupBy(context.Issues, context.Options.GroupBy!);
        
        debugOutput.WriteData("Created groups", groups.Select(g => $"{g.Key}='{g.Value}' ({g.Issues.Count} issues)"));
        
        return Task.FromResult(new ProcessingResult
        {
            Issues = context.Issues,
            Groups = groups
        });
    }
}

public class GroupingEngine : IGroupingEngine
{
    private readonly IFieldAccessor _fieldAccessor;

    public GroupingEngine(IFieldAccessor fieldAccessor)
    {
        _fieldAccessor = fieldAccessor;
    }

    public IReadOnlyList<DataGroup> GroupBy(IEnumerable<Issue> issues, IReadOnlyList<string> groupByFields)
    {
        if (!groupByFields.Any())
            return [];

        return GroupByRecursive(issues, groupByFields, 0);
    }

    public IReadOnlyList<DataGroup> GroupBy(IEnumerable<Issue> issues, string groupByExpression)
    {
        return GroupBy(issues, [groupByExpression]);
    }

    private IReadOnlyList<DataGroup> GroupByRecursive(IEnumerable<Issue> issues, IReadOnlyList<string> groupByFields, int depth)
    {
        if (depth >= groupByFields.Count)
            return [];

        var currentField = groupByFields[depth];
        var grouped = issues
            .GroupBy(issue => _fieldAccessor.GetValue(issue, currentField))
            .ToList();

        var result = new List<DataGroup>();

        foreach (var group in grouped)
        {
            var groupValue = group.Key ?? "Unassigned";
            var groupIssues = group.ToList();
            
            var subGroups = depth + 1 < groupByFields.Count 
                ? GroupByRecursive(groupIssues, groupByFields, depth + 1)
                : [];

            result.Add(new DataGroup(
                currentField,
                groupValue,
                groupIssues,
                subGroups,
                null
            ));
        }

        return result;
    }
}