using JQLBridge.Core.Domain;

namespace JQLBridge.Core.PostProcessing;

public interface IDataProcessingPipeline
{
    Task<ProcessingResult> ProcessAsync(ProcessingContext context);
    void RegisterProcessor(IDataProcessor processor);
}

public class DataProcessingPipeline : IDataProcessingPipeline
{
    private readonly List<IDataProcessor> _processors = new();

    public void RegisterProcessor(IDataProcessor processor)
    {
        _processors.Add(processor);
        _processors.Sort((a, b) => a.Order.CompareTo(b.Order));
    }

    public async Task<ProcessingResult> ProcessAsync(ProcessingContext context)
    {
        IDebugOutput debugOutput = context.Options.CustomOptions.ContainsKey("debug") && (bool)context.Options.CustomOptions["debug"] 
            ? new ConsoleDebugOutput(true) 
            : new NullDebugOutput();

        var currentResult = new ProcessingResult { Issues = context.Issues };
        var applicableProcessors = _processors.Where(p => p.CanProcess(context)).ToList();
        
        debugOutput.WriteSubStep($"Found {applicableProcessors.Count} applicable processors");
        
        foreach (var processor in applicableProcessors)
        {
            var processorName = processor.GetType().Name;
            debugOutput.WriteSubStep($"Running {processorName} (order: {processor.Order})");
            
            var processorResult = await processor.ProcessAsync(context);
            
            debugOutput.WriteData($"{processorName} result", new
            {
                Groups = processorResult.Groups.Count,
                Calculations = processorResult.Calculations.Count,
                Aggregations = processorResult.Aggregations.Count
            });
            
            currentResult = MergeResults(currentResult, processorResult);
        }

        return currentResult;
    }

    private static ProcessingResult MergeResults(ProcessingResult current, ProcessingResult next)
    {
        var mergedCalculations = new Dictionary<string, object>(current.Calculations);
        foreach (var calc in next.Calculations)
        {
            mergedCalculations[calc.Key] = calc.Value;
        }

        var mergedAggregations = new Dictionary<string, object>(current.Aggregations);
        foreach (var agg in next.Aggregations)
        {
            mergedAggregations[agg.Key] = agg.Value;
        }

        var mergedMetadata = new Dictionary<string, object>(current.Metadata);
        foreach (var meta in next.Metadata)
        {
            mergedMetadata[meta.Key] = meta.Value;
        }

        return new ProcessingResult
        {
            Issues = next.Issues.Any() ? next.Issues : current.Issues,
            Groups = next.Groups.Any() ? next.Groups : current.Groups,
            Calculations = mergedCalculations,
            Aggregations = mergedAggregations,
            Metadata = mergedMetadata
        };
    }
}