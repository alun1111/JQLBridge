using JQLBridge.Core.Domain;

namespace JQLBridge.Core.PostProcessing;

public interface IDataProcessor
{
    Task<ProcessingResult> ProcessAsync(ProcessingContext context);
    bool CanProcess(ProcessingContext context);
    int Order { get; }
}

public class ProcessingContext
{
    public IReadOnlyList<Issue> Issues { get; init; } = [];
    public QueryIntent OriginalIntent { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
    public ProcessingOptions Options { get; init; } = new();
}

public class ProcessingOptions
{
    public IReadOnlyList<string>? GroupBy { get; init; }
    public IReadOnlyList<string>? Calculate { get; init; }
    public IReadOnlyList<string>? Aggregate { get; init; }
    public string? OutputFormat { get; init; }
    public Dictionary<string, object> CustomOptions { get; init; } = new();
}

public class ProcessingResult
{
    public IReadOnlyList<Issue> Issues { get; init; } = [];
    public IReadOnlyList<DataGroup> Groups { get; init; } = [];
    public IReadOnlyDictionary<string, object> Calculations { get; init; } = new Dictionary<string, object>();
    public IReadOnlyDictionary<string, object> Aggregations { get; init; } = new Dictionary<string, object>();
    public Dictionary<string, object> Metadata { get; init; } = new();
}

