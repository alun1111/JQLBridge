using JQLBridge.Core.Domain;

namespace JQLBridge.Core.PostProcessing.Processors;

public class CalculationProcessor : IDataProcessor
{
    private readonly ICalculationEngine _calculationEngine;
    
    public int Order => 20;

    public CalculationProcessor(ICalculationEngine calculationEngine)
    {
        _calculationEngine = calculationEngine;
    }

    public bool CanProcess(ProcessingContext context)
    {
        return context.Options.Calculate?.Any() == true;
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

        debugOutput.WriteData("Running calculations", context.Options.Calculate!);
        
        var calculations = _calculationEngine.Calculate(context.Issues, context.Options.Calculate!);
        
        debugOutput.WriteData("Calculation results", calculations);
        
        return Task.FromResult(new ProcessingResult
        {
            Issues = context.Issues,
            Calculations = calculations
        });
    }
}

public class CalculationEngine : ICalculationEngine
{
    private readonly Dictionary<string, Func<IEnumerable<Issue>, object>> _calculations = new();

    public CalculationEngine()
    {
        RegisterDefaultCalculations();
    }

    public IReadOnlyDictionary<string, object> Calculate(IEnumerable<Issue> issues, IReadOnlyList<string> calculations)
    {
        var result = new Dictionary<string, object>();
        var issueList = issues.ToList();

        foreach (var calculation in calculations)
        {
            if (_calculations.TryGetValue(calculation.ToLowerInvariant(), out var calculator))
            {
                result[calculation] = calculator(issueList);
            }
        }

        return result;
    }

    public void RegisterCalculation(string name, Func<IEnumerable<Issue>, object> calculator)
    {
        _calculations[name.ToLowerInvariant()] = calculator;
    }

    public bool CanCalculate(string calculationName)
    {
        return _calculations.ContainsKey(calculationName.ToLowerInvariant());
    }

    private void RegisterDefaultCalculations()
    {
        RegisterCalculation("age", issues => 
            issues.Select(i => (DateTime.UtcNow - i.Created).TotalDays).ToList());
            
        RegisterCalculation("daysSinceUpdate", issues => 
            issues.Select(i => (DateTime.UtcNow - i.Updated).TotalDays).ToList());
            
        RegisterCalculation("velocity", issues => 
            issues.Select(i => {
                var daysSince = (DateTime.UtcNow - i.Updated).TotalDays;
                return daysSince > 0 ? 1.0 / daysSince : 1.0;
            }).ToList());
            
        RegisterCalculation("avgAge", issues => 
            issues.Select(i => (DateTime.UtcNow - i.Created).TotalDays).DefaultIfEmpty(0).Average());
            
        RegisterCalculation("avgDaysSinceUpdate", issues => 
            issues.Select(i => (DateTime.UtcNow - i.Updated).TotalDays).DefaultIfEmpty(0).Average());
            
        RegisterCalculation("totalCount", issues => issues.Count());
        
        RegisterCalculation("statusDistribution", issues => 
            issues.GroupBy(i => i.Status).ToDictionary(g => g.Key.ToString(), g => g.Count()));
    }
}