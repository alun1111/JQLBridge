using JQLBridge.Core.Domain;
using JQLBridge.Core.PostProcessing;
using JQLBridge.Core.PostProcessing.Processors;

namespace JQLBridge.Core.Tests.PostProcessing;

public class GroupingEngineTests
{
    private readonly IGroupingEngine _groupingEngine;
    private readonly List<Issue> _testIssues;

    public GroupingEngineTests()
    {
        _groupingEngine = new GroupingEngine(new FieldAccessor());
        _testIssues = CreateTestIssues();
    }

    [Fact]
    public void GroupBy_Status_ReturnsCorrectGroups()
    {
        var groups = _groupingEngine.GroupBy(_testIssues, ["status"]);

        Assert.Equal(3, groups.Count);
        Assert.Contains(groups, g => g.Value.Equals("ToDo") && g.Issues.Count == 2);
        Assert.Contains(groups, g => g.Value.Equals("InProgress") && g.Issues.Count == 1);
        Assert.Contains(groups, g => g.Value.Equals("Done") && g.Issues.Count == 1);
    }

    [Fact]
    public void GroupBy_MultipleFields_ReturnsNestedGroups()
    {
        var groups = _groupingEngine.GroupBy(_testIssues, ["status", "assignee"]);

        Assert.Equal(3, groups.Count);
        
        var todoGroup = groups.First(g => g.Value.Equals("ToDo"));
        Assert.Equal(2, todoGroup.SubGroups?.Count ?? 0);
        Assert.Contains(todoGroup.SubGroups ?? [], sg => sg.Value.Equals("Alice") && sg.Issues.Count == 1);
        Assert.Contains(todoGroup.SubGroups ?? [], sg => sg.Value.Equals("Bob") && sg.Issues.Count == 1);
    }

    [Fact]
    public void GroupBy_EmptyFields_ReturnsEmptyList()
    {
        var groups = _groupingEngine.GroupBy(_testIssues, []);
        Assert.Empty(groups);
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