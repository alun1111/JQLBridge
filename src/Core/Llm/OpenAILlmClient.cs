using JQLBridge.Core.Domain;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

namespace JQLBridge.Core.Llm;

public class OpenAILlmClient : ILlmClient
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<OpenAILlmClient> _logger;
    private readonly OpenAIConfiguration _configuration;

    private static readonly string SystemPrompt = """
        You are a JIRA Query Language (JQL) assistant that converts natural language queries into structured intent objects.

        Your task is to analyze the user's natural language request and return a JSON intent object that describes what they want to find in JIRA.

        Available fields for filtering:
        - project: Project key (e.g., "BANK", "WEB")
        - assignee: User identifier or "currentUser"
        - reporter: User identifier
        - status: Array of status names (e.g., ["Open", "In Progress", "Done"])
        - priority: Array of priorities (e.g., ["High", "Medium", "Low"])
        - issueTypes: Array of issue types (e.g., ["Bug", "Story", "Task", "Epic"])
        - labels: Array of label names
        - components: Array of component names
        - fixVersions: Array of version names
        - created: Date range object
        - updated: Date range object

        Date range format:
        - { "lastDays": N } for last N days
        - { "after": "YYYY-MM-DD" } for dates after
        - { "before": "YYYY-MM-DD" } for dates before
        - { "between": { "start": "YYYY-MM-DD", "end": "YYYY-MM-DD" } }

        Additional options:
        - search: Text to search in summary/description
        - sort: Array of sort fields [{ "field": "updated", "order": "desc" }]
        - limit: Maximum number of results

        IMPORTANT: Return ONLY valid JSON. Do not include explanations or markdown formatting.

        Examples:

        User: "Show bugs assigned to me updated in the last 7 days"
        Response: {"filters":{"assignee":"currentUser","issueTypes":["Bug"],"updated":{"lastDays":7}}}

        User: "High priority stories in BANK project"
        Response: {"filters":{"project":"BANK","priorities":["High"],"issueTypes":["Story"]}}

        User: "Open issues sorted by updated date"
        Response: {"filters":{"status":["Open","To Do"]},"sort":[{"field":"updated","order":"desc"}]}

        User: "Find tasks with payment in description created this month"
        Response: {"filters":{"issueTypes":["Task"],"created":{"lastDays":30}},"search":"payment"}

        User: "Top 10 recent bugs"
        Response: {"filters":{"issueTypes":["Bug"]},"sort":[{"field":"updated","order":"desc"}],"limit":10}

        Now convert this user query:
        """;

    public OpenAILlmClient(ChatClient chatClient, OpenAIConfiguration configuration, ILogger<OpenAILlmClient> logger)
    {
        _chatClient = chatClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<QueryIntent> ParseNaturalLanguageAsync(string prompt, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Parsing natural language query: {Query}", prompt);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(SystemPrompt),
                new UserChatMessage(prompt)
            };

            var options = new ChatCompletionOptions
            {
                Temperature = _configuration.Temperature,
                MaxOutputTokenCount = _configuration.MaxTokens
            };

            var completion = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
            var response = completion.Value.Content[0].Text;

            _logger.LogDebug("Received LLM response: {Response}", response);

            // Parse the JSON response into QueryIntent
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var intentJson = JsonDocument.Parse(response);
            var intent = ParseQueryIntent(intentJson.RootElement);

            _logger.LogDebug("Parsed intent: {@Intent}", intent);
            return intent;
        }
        catch (ClientResultException ex)
        {
            _logger.LogError(ex, "OpenAI API error while parsing query: {Query}", prompt);
            throw new LlmException("Failed to parse natural language query", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse LLM response as JSON for query: {Query}", prompt);
            throw new LlmException("Invalid response format from LLM", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while parsing query: {Query}", prompt);
            throw new LlmException("Unexpected error during query parsing", ex);
        }
    }

    private static QueryIntent ParseQueryIntent(JsonElement json)
    {
        var filters = new QueryFilters();
        List<SortField>? sort = null;
        int? limit = null;
        string? search = null;

        if (json.TryGetProperty("filters", out var filtersElement))
        {
            filters = ParseQueryFilters(filtersElement);
        }

        if (json.TryGetProperty("search", out var searchElement))
        {
            search = searchElement.GetString();
        }

        if (json.TryGetProperty("sort", out var sortElement) && sortElement.ValueKind == JsonValueKind.Array)
        {
            sort = new List<SortField>();
            foreach (var sortItem in sortElement.EnumerateArray())
            {
                if (sortItem.TryGetProperty("field", out var fieldElement) &&
                    sortItem.TryGetProperty("order", out var orderElement))
                {
                    var field = fieldElement.GetString();
                    var order = orderElement.GetString()?.ToLowerInvariant() == "desc" ? SortOrder.Desc : SortOrder.Asc;
                    
                    if (!string.IsNullOrEmpty(field))
                    {
                        sort.Add(new SortField(field, order));
                    }
                }
            }
        }

        if (json.TryGetProperty("limit", out var limitElement))
        {
            limit = limitElement.GetInt32();
        }

        return new QueryIntent(
            Filters: filters,
            Search: search,
            Sort: sort,
            Limit: limit);
    }

    private static QueryFilters ParseQueryFilters(JsonElement filtersElement)
    {
        string? project = null;
        string? assignee = null;
        IReadOnlyList<string>? status = null;
        IReadOnlyList<string>? priorities = null;
        IReadOnlyList<string>? issueTypes = null;
        IReadOnlyList<string>? labels = null;
        IReadOnlyList<string>? components = null;
        DateRange? created = null;
        DateRange? updated = null;

        if (filtersElement.TryGetProperty("project", out var projectElement))
        {
            project = projectElement.GetString();
        }

        if (filtersElement.TryGetProperty("assignee", out var assigneeElement))
        {
            assignee = assigneeElement.GetString();
        }


        if (filtersElement.TryGetProperty("status", out var statusElement) && statusElement.ValueKind == JsonValueKind.Array)
        {
            status = statusElement.EnumerateArray().Select(x => x.GetString()).Where(x => x != null).Cast<string>().ToList();
        }

        if (filtersElement.TryGetProperty("priorities", out var prioritiesElement) && prioritiesElement.ValueKind == JsonValueKind.Array)
        {
            priorities = prioritiesElement.EnumerateArray().Select(x => x.GetString()).Where(x => x != null).Cast<string>().ToList();
        }

        if (filtersElement.TryGetProperty("issueTypes", out var issueTypesElement) && issueTypesElement.ValueKind == JsonValueKind.Array)
        {
            issueTypes = issueTypesElement.EnumerateArray().Select(x => x.GetString()).Where(x => x != null).Cast<string>().ToList();
        }

        if (filtersElement.TryGetProperty("labels", out var labelsElement) && labelsElement.ValueKind == JsonValueKind.Array)
        {
            labels = labelsElement.EnumerateArray().Select(x => x.GetString()).Where(x => x != null).Cast<string>().ToList();
        }

        if (filtersElement.TryGetProperty("components", out var componentsElement) && componentsElement.ValueKind == JsonValueKind.Array)
        {
            components = componentsElement.EnumerateArray().Select(x => x.GetString()).Where(x => x != null).Cast<string>().ToList();
        }


        if (filtersElement.TryGetProperty("created", out var createdElement))
        {
            created = ParseDateRange(createdElement);
        }

        if (filtersElement.TryGetProperty("updated", out var updatedElement))
        {
            updated = ParseDateRange(updatedElement);
        }

        return new QueryFilters(
            Project: project,
            Assignee: assignee,
            Status: status,
            Updated: updated,
            Created: created,
            Labels: labels,
            Components: components,
            IssueTypes: issueTypes,
            Priorities: priorities);
    }

    private static DateRange? ParseDateRange(JsonElement dateElement)
    {
        if (dateElement.TryGetProperty("lastDays", out var lastDaysElement))
        {
            return new DateRange(LastDays: lastDaysElement.GetInt32());
        }

        if (dateElement.TryGetProperty("after", out var afterElement))
        {
            var after = DateTime.Parse(afterElement.GetString()!);
            return new DateRange(From: after);
        }

        if (dateElement.TryGetProperty("before", out var beforeElement))
        {
            var before = DateTime.Parse(beforeElement.GetString()!);
            return new DateRange(To: before);
        }

        if (dateElement.TryGetProperty("between", out var betweenElement))
        {
            if (betweenElement.TryGetProperty("start", out var startElement) &&
                betweenElement.TryGetProperty("end", out var endElement))
            {
                var start = DateTime.Parse(startElement.GetString()!);
                var end = DateTime.Parse(endElement.GetString()!);
                return new DateRange(From: start, To: end);
            }
        }

        return null;
    }
}

public record OpenAIConfiguration(
    string ApiKey,
    string Model = "gpt-4o",
    float Temperature = 0.1f,
    int MaxTokens = 1000);

public class LlmException : Exception
{
    public LlmException(string message) : base(message) { }
    public LlmException(string message, Exception innerException) : base(message, innerException) { }
}