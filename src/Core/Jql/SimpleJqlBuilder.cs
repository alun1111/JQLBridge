using JQLBridge.Core.Domain;

namespace JQLBridge.Core.Jql;

public interface ISimpleJqlBuilder
{
    JqlQuery BuildQuery(QueryIntent intent);
}

public class SimpleJqlBuilder : ISimpleJqlBuilder
{
    public JqlQuery BuildQuery(QueryIntent intent)
    {
        var whereClauses = new List<string>();
        
        // Build WHERE clauses from filters
        if (intent.Filters != null)
        {
            AddProjectFilter(whereClauses, intent.Filters.Project);
            AddAssigneeFilter(whereClauses, intent.Filters.Assignee);
            AddStatusFilter(whereClauses, intent.Filters.Status);
            AddIssueTypeFilter(whereClauses, intent.Filters.IssueTypes);
            AddDateRangeFilter(whereClauses, intent.Filters.Updated, "updated");
            AddDateRangeFilter(whereClauses, intent.Filters.Created, "created");
            AddPriorityFilter(whereClauses, intent.Filters.Priorities);
        }
        
        // Add text search if specified
        if (!string.IsNullOrEmpty(intent.Search))
        {
            whereClauses.Add($"text ~ \"{intent.Search}\"");
        }
        
        // Combine all WHERE clauses
        var whereClause = whereClauses.Any() 
            ? string.Join(" AND ", whereClauses.Select(c => $"({c})"))
            : "";
            
        // Build final JQL query
        var jql = string.IsNullOrEmpty(whereClause) ? "" : whereClause;
        
        return new JqlQuery(
            Query: jql,
            MaxResults: intent.Limit,
            StartAt: null
        );
    }
    
    private static void AddProjectFilter(List<string> whereClauses, string? project)
    {
        if (!string.IsNullOrEmpty(project))
        {
            whereClauses.Add($"project = \"{project}\"");
        }
    }
    
    private static void AddAssigneeFilter(List<string> whereClauses, string? assignee)
    {
        if (!string.IsNullOrEmpty(assignee))
        {
            var clause = assignee.ToLowerInvariant() switch
            {
                "currentuser" => "assignee = currentUser()",
                "unassigned" => "assignee is EMPTY",
                _ => $"assignee = \"{assignee}\""
            };
            whereClauses.Add(clause);
        }
    }
    
    private static void AddStatusFilter(List<string> whereClauses, IReadOnlyList<string>? statuses)
    {
        if (statuses?.Any() == true)
        {
            if (statuses.Count == 1)
            {
                whereClauses.Add($"status = \"{statuses[0]}\"");
            }
            else
            {
                var statusList = string.Join(", ", statuses.Select(s => $"\"{s}\""));
                whereClauses.Add($"status IN ({statusList})");
            }
        }
    }
    
    private static void AddIssueTypeFilter(List<string> whereClauses, IReadOnlyList<string>? issueTypes)
    {
        if (issueTypes?.Any() == true)
        {
            if (issueTypes.Count == 1)
            {
                whereClauses.Add($"type = \"{issueTypes[0]}\"");
            }
            else
            {
                var typeList = string.Join(", ", issueTypes.Select(t => $"\"{t}\""));
                whereClauses.Add($"type IN ({typeList})");
            }
        }
    }
    
    private static void AddPriorityFilter(List<string> whereClauses, IReadOnlyList<string>? priorities)
    {
        if (priorities?.Any() == true)
        {
            if (priorities.Count == 1)
            {
                whereClauses.Add($"priority = \"{priorities[0]}\"");
            }
            else
            {
                var priorityList = string.Join(", ", priorities.Select(p => $"\"{p}\""));
                whereClauses.Add($"priority IN ({priorityList})");
            }
        }
    }
    
    private static void AddDateRangeFilter(List<string> whereClauses, DateRange? dateRange, string fieldName)
    {
        if (dateRange == null) return;
        
        if (dateRange.LastDays.HasValue)
        {
            whereClauses.Add($"{fieldName} >= -{dateRange.LastDays}d");
        }
        else
        {
            if (dateRange.From.HasValue)
            {
                whereClauses.Add($"{fieldName} >= \"{dateRange.From:yyyy-MM-dd}\"");
            }
            if (dateRange.To.HasValue)
            {
                whereClauses.Add($"{fieldName} <= \"{dateRange.To:yyyy-MM-dd}\"");
            }
        }
    }
}

public record JqlQuery(
    string Query,
    int? MaxResults = null,
    int? StartAt = null);