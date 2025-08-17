using JQLBridge.Core.Domain;
using JQLBridge.Core.PostProcessing;
using JQLBridge.Core.PostProcessing.Processors;

namespace JQLBridge.Core.Tests.PostProcessing;

public class CalculationEngineTests
{
    private readonly ICalculationEngine _calculationEngine;
    private readonly List<Issue> _testIssues;

    public CalculationEngineTests()
    {
        _calculationEngine = new CalculationEngine();
        _testIssues = CreateTestIssues();
    }

    [Fact]
    public void Calculate_Age_ReturnsCorrectValues()
    {
        var results = _calculationEngine.Calculate(_testIssues, ["age"]);

        Assert.True(results.ContainsKey("age"));
        var ages = results["age"] as List<double>;
        Assert.NotNull(ages);
        Assert.Equal(4, ages.Count);
        Assert.All(ages, age => Assert.True(age > 0));
    }

    [Fact]
    public void Calculate_AverageAge_ReturnsCorrectValue()
    {
        var results = _calculationEngine.Calculate(_testIssues, ["avgAge"]);

        Assert.True(results.ContainsKey("avgAge"));
        var avgAge = (double)results["avgAge"];
        Assert.True(avgAge > 0);
    }

    [Fact]
    public void Calculate_StatusDistribution_ReturnsCorrectCounts()
    {
        var results = _calculationEngine.Calculate(_testIssues, ["statusDistribution"]);

        Assert.True(results.ContainsKey("statusDistribution"));
        var distribution = results["statusDistribution"] as Dictionary<string, int>;
        Assert.NotNull(distribution);
        Assert.Equal(2, distribution["ToDo"]);
        Assert.Equal(1, distribution["InProgress"]);
        Assert.Equal(1, distribution["Done"]);
    }

    [Fact]
    public void CanCalculate_ValidCalculation_ReturnsTrue()
    {
        Assert.True(_calculationEngine.CanCalculate("age"));
        Assert.True(_calculationEngine.CanCalculate("velocity"));
        Assert.True(_calculationEngine.CanCalculate("avgAge"));
    }

    [Fact]
    public void CanCalculate_InvalidCalculation_ReturnsFalse()
    {
        Assert.False(_calculationEngine.CanCalculate("invalidCalculation"));
    }

    private static List<Issue> CreateTestIssues()
    {
        return
        [
            new Issue(
                Id: "1",
                Key: "TEST-1",
                Summary: "First issue",
                Description: "First test issue",
                Status: IssueStatus.ToDo,
                Priority: IssuePriority.High,
                Type: IssueType.Bug,
                Assignee: new User("1", "Alice", "alice@example.com"),
                Reporter: new User("1", "Alice", "alice@example.com"),
                Project: "TEST",
                Created: DateTime.UtcNow.AddDays(-5),
                Updated: DateTime.UtcNow.AddDays(-2),
                ResolutionDate: null,
                Labels: [],
                Components: [],
                FixVersions: [],
                Resolution: null
            ),
            new Issue(
                Id: "2",
                Key: "TEST-2",
                Summary: "Second issue",
                Description: "Second test issue",
                Status: IssueStatus.ToDo,
                Priority: IssuePriority.Medium,
                Type: IssueType.Task,
                Assignee: new User("2", "Bob", "bob@example.com"),
                Reporter: new User("2", "Bob", "bob@example.com"),
                Project: "TEST",
                Created: DateTime.UtcNow.AddDays(-3),
                Updated: DateTime.UtcNow.AddDays(-1),
                ResolutionDate: null,
                Labels: [],
                Components: [],
                FixVersions: [],
                Resolution: null
            ),
            new Issue(
                Id: "3",
                Key: "TEST-3",
                Summary: "Third issue",
                Description: "Third test issue",
                Status: IssueStatus.InProgress,
                Priority: IssuePriority.Low,
                Type: IssueType.Story,
                Assignee: new User("3", "Charlie", "charlie@example.com"),
                Reporter: new User("3", "Charlie", "charlie@example.com"),
                Project: "TEST",
                Created: DateTime.UtcNow.AddDays(-7),
                Updated: DateTime.UtcNow,
                ResolutionDate: null,
                Labels: [],
                Components: [],
                FixVersions: [],
                Resolution: null
            ),
            new Issue(
                Id: "4",
                Key: "TEST-4",
                Summary: "Fourth issue",
                Description: "Fourth test issue",
                Status: IssueStatus.Done,
                Priority: IssuePriority.High,
                Type: IssueType.Bug,
                Assignee: new User("1", "Alice", "alice@example.com"),
                Reporter: new User("1", "Alice", "alice@example.com"),
                Project: "TEST",
                Created: DateTime.UtcNow.AddDays(-10),
                Updated: DateTime.UtcNow.AddDays(-1),
                ResolutionDate: null,
                Labels: [],
                Components: [],
                FixVersions: [],
                Resolution: null
            )
        ];
    }
}