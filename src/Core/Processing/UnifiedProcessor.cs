using JQLBridge.Core.Domain;

namespace JQLBridge.Core.Processing;

public interface IUnifiedProcessor
{
    Task<ProcessingResult> ProcessAsync(ProcessingContext context);
}

public class UnifiedProcessor : IUnifiedProcessor
{
    private readonly IFieldAccessor _fieldAccessor;

    public UnifiedProcessor(IFieldAccessor fieldAccessor)
    {
        _fieldAccessor = fieldAccessor;
    }

    public async Task<ProcessingResult> ProcessAsync(ProcessingContext context)
    {
        var result = new ProcessingResult { Issues = context.Issues };
        
        var debugOutput = GetDebugOutput(context);
        debugOutput.WriteStep("🔄 Starting unified post-processing");

        // Apply sorting first (moved from query engine)
        var sortedIssues = ApplySorting(context.Issues, context.OriginalIntent?.Sort);
        var groups = context.Options.GroupBy?.Any() == true 
            ? ApplyGrouping(sortedIssues, context.Options.GroupBy!)
            : new List<DataGroup>();
            
        var calculations = context.Options.Calculate?.Any() == true
            ? ApplyCalculations(sortedIssues, context.Options.Calculate!, groups)
            : (IReadOnlyDictionary<string, object>)new Dictionary<string, object>();
            
        var aggregations = context.Options.Aggregate?.Any() == true
            ? ApplyAggregations(sortedIssues, context.Options.Aggregate!, groups)
            : (IReadOnlyDictionary<string, object>)new Dictionary<string, object>();
            
        result = new ProcessingResult
        {
            Issues = sortedIssues,
            Groups = groups,
            Calculations = calculations,
            Aggregations = aggregations
        };
        
        if (context.Options.GroupBy?.Any() == true)
        {
            debugOutput.WriteSubStep("Grouping by fields", context.Options.GroupBy);
            debugOutput.WriteSubStep("Created groups", result.Groups.Count);
        }

        if (context.Options.Calculate?.Any() == true)
        {
            debugOutput.WriteSubStep("Running calculations", context.Options.Calculate);
            debugOutput.WriteSubStep("Completed calculations", result.Calculations.Count);
        }

        if (context.Options.Aggregate?.Any() == true)
        {
            debugOutput.WriteSubStep("Running aggregations", context.Options.Aggregate);
            debugOutput.WriteSubStep("Completed aggregations", result.Aggregations.Count);
        }

        debugOutput.WriteStep("✅ Unified post-processing complete");
        return result;
    }

    private static IDebugOutput GetDebugOutput(ProcessingContext context)
    {
        return context.Options.CustomOptions.ContainsKey("debug") && (bool)context.Options.CustomOptions["debug"] 
            ? new ConsoleDebugOutput(true) 
            : new NullDebugOutput();
    }

    private static IReadOnlyList<Issue> ApplySorting(IReadOnlyList<Issue> issues, IReadOnlyList<SortField>? sortFields)
    {
        if (sortFields?.Any() != true) return issues;

        var ordered = issues.AsEnumerable();
        
        foreach (var sortField in sortFields)
        {
            ordered = sortField.Field.ToLowerInvariant() switch
            {
                "updated" => sortField.Order == SortOrder.Desc 
                    ? ordered.OrderByDescending(i => i.Updated)
                    : ordered.OrderBy(i => i.Updated),
                "created" => sortField.Order == SortOrder.Desc
                    ? ordered.OrderByDescending(i => i.Created) 
                    : ordered.OrderBy(i => i.Created),
                "priority" => sortField.Order == SortOrder.Desc
                    ? ordered.OrderByDescending(i => i.Priority)
                    : ordered.OrderBy(i => i.Priority),
                "status" => sortField.Order == SortOrder.Desc
                    ? ordered.OrderByDescending(i => i.Status)
                    : ordered.OrderBy(i => i.Status),
                _ => ordered
            };
        }
        
        return ordered.ToList();
    }

    private IReadOnlyList<DataGroup> ApplyGrouping(IReadOnlyList<Issue> issues, IReadOnlyList<string> groupByFields)
    {
        if (!groupByFields.Any()) return [];

        return GroupByRecursive(issues, groupByFields, 0);
    }

    private IReadOnlyList<DataGroup> GroupByRecursive(IEnumerable<Issue> issues, IReadOnlyList<string> groupByFields, int depth)
    {
        if (depth >= groupByFields.Count) return [];

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

    private IReadOnlyDictionary<string, object> ApplyCalculations(IReadOnlyList<Issue> issues, IReadOnlyList<string> calculations, IReadOnlyList<DataGroup> groups)
    {
        var results = new Dictionary<string, object>();

        foreach (var calculation in calculations)
        {
            results[calculation] = calculation.ToLowerInvariant() switch
            {
                "age" => CalculateAges(issues),
                "velocity" => CalculateVelocity(issues),
                "avgage" => CalculateAverageAge(issues),
                "statusdistribution" => CalculateStatusDistribution(issues),
                _ => $"Unknown calculation: {calculation}"
            };
        }

        return results;
    }

    private IReadOnlyDictionary<string, object> ApplyAggregations(IReadOnlyList<Issue> issues, IReadOnlyList<string> aggregations, IReadOnlyList<DataGroup> groups)
    {
        var results = new Dictionary<string, object>();

        foreach (var aggregation in aggregations)
        {
            results[aggregation] = aggregation.ToLowerInvariant() switch
            {
                "count" => issues.Count,
                "avg_age" => CalculateAverageAge(issues),
                "status_counts" => CalculateStatusCounts(issues),
                "assignee_counts" => CalculateAssigneeCounts(issues),
                "priority_counts" => CalculatePriorityCounts(issues),
                _ => $"Unknown aggregation: {aggregation}"
            };
        }

        return results;
    }

    private static object CalculateAges(IReadOnlyList<Issue> issues)
    {
        return issues.Select(i => new { 
            Key = i.Key, 
            Age = (DateTime.Now - i.Created).Days 
        }).ToList();
    }

    private static double CalculateAverageAge(IReadOnlyList<Issue> issues)
    {
        if (!issues.Any()) return 0;
        return issues.Average(i => (DateTime.Now - i.Created).TotalDays);
    }

    private static object CalculateVelocity(IReadOnlyList<Issue> issues)
    {
        // Simple velocity calculation - issues completed per week
        var completedIssues = issues.Where(i => 
            i.Status == IssueStatus.Done ||
            i.Status == IssueStatus.Closed).ToList();
            
        if (!completedIssues.Any()) return 0;
        
        var weeks = completedIssues
            .GroupBy(i => GetWeekOfYear(i.Updated))
            .Count();
            
        return weeks > 0 ? (double)completedIssues.Count / weeks : 0;
    }

    private static object CalculateStatusDistribution(IReadOnlyList<Issue> issues)
    {
        return issues
            .GroupBy(i => i.Status)
            .ToDictionary(g => g.Key, g => new { Count = g.Count(), Percentage = (double)g.Count() / issues.Count * 100 });
    }

    private static Dictionary<string, int> CalculateStatusCounts(IReadOnlyList<Issue> issues)
    {
        return issues
            .GroupBy(i => i.Status)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());
    }

    private static Dictionary<string, int> CalculateAssigneeCounts(IReadOnlyList<Issue> issues)
    {
        return issues
            .GroupBy(i => i.Assignee?.DisplayName ?? "Unassigned")
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private static Dictionary<string, int> CalculatePriorityCounts(IReadOnlyList<Issue> issues)
    {
        return issues
            .GroupBy(i => i.Priority)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());
    }

    private static int GetWeekOfYear(DateTime date)
    {
        var calendar = System.Globalization.CultureInfo.CurrentCulture.Calendar;
        return calendar.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
    }
}