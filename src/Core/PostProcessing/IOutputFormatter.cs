using JQLBridge.Core.Domain;

namespace JQLBridge.Core.PostProcessing;

public interface IOutputFormatter
{
    string Format { get; }
    Task<string> FormatAsync(QueryResult result, ProcessingResult? processed = null);
    bool CanFormat(string format);
}

public class OutputFormatterRegistry
{
    private readonly Dictionary<string, IOutputFormatter> _formatters = new();

    public void RegisterFormatter(IOutputFormatter formatter)
    {
        _formatters[formatter.Format.ToLowerInvariant()] = formatter;
    }

    public IOutputFormatter? GetFormatter(string format)
    {
        return _formatters.TryGetValue(format.ToLowerInvariant(), out var formatter) ? formatter : null;
    }

    public IEnumerable<string> GetAvailableFormats()
    {
        return _formatters.Keys;
    }
}