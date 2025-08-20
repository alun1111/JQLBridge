using JQLBridge.Core.Domain;

namespace JQLBridge.Core.Processing;

public record ProcessingContext(
    IReadOnlyList<Issue> Issues,
    QueryIntent? OriginalIntent,
    ProcessingOptions Options);

public record ProcessingOptions(
    IReadOnlyList<string>? GroupBy = null,
    IReadOnlyList<string>? Calculate = null,
    IReadOnlyList<string>? Aggregate = null,
    string? OutputFormat = null,
    IDictionary<string, object>? CustomOptions = null)
{
    public IDictionary<string, object> CustomOptions { get; init; } = CustomOptions ?? new Dictionary<string, object>();
}

public record ProcessingResult(
    IReadOnlyList<Issue>? Issues = null,
    IReadOnlyList<DataGroup>? Groups = null,
    IReadOnlyDictionary<string, object>? Calculations = null,
    IReadOnlyDictionary<string, object>? Aggregations = null,
    IReadOnlyDictionary<string, object>? Metadata = null)
{
    public IReadOnlyList<Issue> Issues { get; init; } = Issues ?? [];
    public IReadOnlyList<DataGroup> Groups { get; init; } = Groups ?? [];
    public IReadOnlyDictionary<string, object> Calculations { get; init; } = Calculations ?? new Dictionary<string, object>();
    public IReadOnlyDictionary<string, object> Aggregations { get; init; } = Aggregations ?? new Dictionary<string, object>();
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = Metadata ?? new Dictionary<string, object>();
}

public interface IFieldAccessor
{
    object? GetValue(Issue issue, string fieldName);
}

public class FieldAccessor : IFieldAccessor
{
    public object? GetValue(Issue issue, string fieldName)
    {
        return fieldName.ToLowerInvariant() switch
        {
            "status" => issue.Status,
            "assignee" => issue.Assignee?.DisplayName ?? "Unassigned",
            "priority" => issue.Priority.ToString(),
            "project" => issue.Project,
            "type" => issue.Type.ToString(),
            "created" => issue.Created,
            "updated" => issue.Updated,
            "key" => issue.Key,
            "summary" => issue.Summary,
            _ => null
        };
    }
}

public interface IDebugOutput
{
    void WriteStep(string message, object? data = null);
    void WriteSubStep(string message, object? data = null);
    void WriteData(string label, object data);
}

public class NullDebugOutput : IDebugOutput
{
    public void WriteStep(string message, object? data = null) { }
    public void WriteSubStep(string message, object? data = null) { }
    public void WriteData(string label, object data) { }
}

public class ConsoleDebugOutput : IDebugOutput
{
    private readonly bool _enabled;

    public ConsoleDebugOutput(bool enabled)
    {
        _enabled = enabled;
    }

    public void WriteStep(string message, object? data = null)
    {
        if (!_enabled) return;
        Console.WriteLine();
        Console.WriteLine($"{message}");
        if (data != null)
        {
            Console.WriteLine($"  Input: {System.Text.Json.JsonSerializer.Serialize(data)}");
        }
    }

    public void WriteSubStep(string message, object? data = null)
    {
        if (!_enabled) return;
        Console.WriteLine($"  ↳ {message}");
        if (data != null)
        {
            Console.WriteLine($"    {System.Text.Json.JsonSerializer.Serialize(data)}");
        }
    }

    public void WriteData(string label, object data)
    {
        if (!_enabled) return;
        Console.WriteLine($"  📊 {label}: {System.Text.Json.JsonSerializer.Serialize(data)}");
    }
}