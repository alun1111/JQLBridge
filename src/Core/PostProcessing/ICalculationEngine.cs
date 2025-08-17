using JQLBridge.Core.Domain;

namespace JQLBridge.Core.PostProcessing;

public interface ICalculationEngine
{
    IReadOnlyDictionary<string, object> Calculate(IEnumerable<Issue> issues, IReadOnlyList<string> calculations);
    void RegisterCalculation(string name, Func<IEnumerable<Issue>, object> calculator);
    bool CanCalculate(string calculationName);
}

public interface ICalculationFunction
{
    string Name { get; }
    object Calculate(IEnumerable<Issue> issues);
    object Calculate(Issue issue);
}

public class AgeCalculation : ICalculationFunction
{
    public string Name => "age";
    
    public object Calculate(IEnumerable<Issue> issues)
    {
        return issues.Select(Calculate).ToList();
    }
    
    public object Calculate(Issue issue)
    {
        return (DateTime.UtcNow - issue.Created).TotalDays;
    }
}

public class VelocityCalculation : ICalculationFunction
{
    public string Name => "velocity";
    
    public object Calculate(IEnumerable<Issue> issues)
    {
        return issues.Select(Calculate).ToList();
    }
    
    public object Calculate(Issue issue)
    {
        var daysSinceUpdate = (DateTime.UtcNow - issue.Updated).TotalDays;
        return daysSinceUpdate > 0 ? 1.0 / daysSinceUpdate : 1.0;
    }
}

public class TimeSinceUpdateCalculation : ICalculationFunction
{
    public string Name => "daysSinceUpdate";
    
    public object Calculate(IEnumerable<Issue> issues)
    {
        return issues.Select(Calculate).ToList();
    }
    
    public object Calculate(Issue issue)
    {
        return (DateTime.UtcNow - issue.Updated).TotalDays;
    }
}