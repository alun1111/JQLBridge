namespace JQLBridge.Core.PostProcessing;

public interface IDebugOutput
{
    void WriteStep(string step, object? data = null);
    void WriteSubStep(string subStep, object? data = null);
    void WriteData(string label, object data);
    bool IsEnabled { get; }
}

public class ConsoleDebugOutput : IDebugOutput
{
    public bool IsEnabled { get; }

    public ConsoleDebugOutput(bool enabled)
    {
        IsEnabled = enabled;
    }

    public void WriteStep(string step, object? data = null)
    {
        if (!IsEnabled) return;
        
        Console.WriteLine($"🔍 {step}");
        if (data != null)
        {
            WriteData("", data);
        }
    }

    public void WriteSubStep(string subStep, object? data = null)
    {
        if (!IsEnabled) return;
        
        Console.WriteLine($"   ▶ {subStep}");
        if (data != null)
        {
            WriteData("   ", data);
        }
    }

    public void WriteData(string label, object data)
    {
        if (!IsEnabled) return;
        
        var prefix = string.IsNullOrEmpty(label) ? "   📊 " : $"   📊 {label}: ";
        
        switch (data)
        {
            case string str:
                Console.WriteLine($"{prefix}{str}");
                break;
            case int count:
                Console.WriteLine($"{prefix}{count}");
                break;
            case IEnumerable<string> strings:
                Console.WriteLine($"{prefix}[{string.Join(", ", strings)}]");
                break;
            case IDictionary<string, object> dict:
                Console.WriteLine($"{prefix}{dict.Count} items");
                foreach (var kvp in dict.Take(3))
                {
                    Console.WriteLine($"     - {kvp.Key}: {kvp.Value}");
                }
                if (dict.Count > 3)
                {
                    Console.WriteLine($"     ... and {dict.Count - 3} more");
                }
                break;
            default:
                Console.WriteLine($"{prefix}{data}");
                break;
        }
    }
}

public class NullDebugOutput : IDebugOutput
{
    public bool IsEnabled => false;
    public void WriteStep(string step, object? data = null) { }
    public void WriteSubStep(string subStep, object? data = null) { }
    public void WriteData(string label, object data) { }
}