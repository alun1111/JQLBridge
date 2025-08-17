using JQLBridge.Core.Domain;

namespace JQLBridge.Core.PostProcessing;

public interface IGroupingEngine
{
    IReadOnlyList<DataGroup> GroupBy(IEnumerable<Issue> issues, IReadOnlyList<string> groupByFields);
    IReadOnlyList<DataGroup> GroupBy(IEnumerable<Issue> issues, string groupByExpression);
}

public interface IFieldAccessor
{
    object? GetValue(Issue issue, string fieldPath);
    bool CanAccess(string fieldPath);
}

public class FieldAccessor : IFieldAccessor
{
    public object? GetValue(Issue issue, string fieldPath)
    {
        return fieldPath.ToLowerInvariant() switch
        {
            "key" => issue.Key,
            "summary" => issue.Summary,
            "status" => issue.Status.ToString(),
            "assignee" => issue.Assignee?.DisplayName,
            "assignee.email" => issue.Assignee?.EmailAddress,
            "priority" => issue.Priority.ToString(),
            "created" => issue.Created,
            "updated" => issue.Updated,
            "issuetype" or "type" => issue.Type.ToString(),
            "project" => issue.Project,
            "labels" => issue.Labels,
            "components" => issue.Components,
            "reporter" => issue.Reporter?.DisplayName,
            "reporter.email" => issue.Reporter?.EmailAddress,
            _ => null
        };
    }

    public bool CanAccess(string fieldPath)
    {
        return GetValue(new Issue(
            Id: "", Key: "", Summary: "", Description: "", Status: IssueStatus.ToDo, Priority: IssuePriority.Medium,
            Type: IssueType.Task, Assignee: null, Reporter: new User("", "", ""), Project: "",
            Created: DateTime.Now, Updated: DateTime.Now, ResolutionDate: null,
            Labels: [], Components: [], FixVersions: [], Resolution: null
        ), fieldPath) != null;
    }
}