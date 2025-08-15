using JQLBridge.Core.Domain;
using JQLBridge.Core.Jira;
using JQLBridge.Core.Llm;
using JQLBridge.Core.QueryEngine;
using JQLBridge.Core.QueryEngine.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Services.Configure<AppConfiguration>(builder.Configuration.GetSection("App"));

RegisterServices(builder.Services, builder.Configuration);

var app = builder.Build();

var queryRunner = app.Services.GetRequiredService<QueryRunner>();

if (args.Length == 0)
{
    AnsiConsole.MarkupLine("[red]Usage: JQLBridge.Cli \"<natural language query>\"[/]");
    AnsiConsole.MarkupLine("[yellow]Example: JQLBridge.Cli \"show bugs assigned to me updated last 7 days\"[/]");
    return 1;
}

var query = string.Join(" ", args);

try
{
    var result = await queryRunner.RunQueryAsync(query);
    
    AnsiConsole.MarkupLine($"[green]Generated JQL:[/] {result.GeneratedJql}");
    AnsiConsole.WriteLine();
    
    if (result.Issues.Any())
    {
        var table = new Table()
            .BorderColor(Color.Grey)
            .AddColumn("Key")
            .AddColumn("Summary")
            .AddColumn("Status")
            .AddColumn("Assignee")
            .AddColumn("Updated");

        foreach (var issue in result.Issues.Take(20))
        {
            table.AddRow(
                issue.Key,
                issue.Summary.Length > 50 ? issue.Summary.Substring(0, 47) + "..." : issue.Summary,
                issue.Status.ToString(),
                issue.Assignee?.DisplayName ?? "Unassigned",
                issue.Updated.ToString("yyyy-MM-dd"));
        }

        AnsiConsole.Write(table);
        
        if (result.Issues.Count > 20)
        {
            AnsiConsole.MarkupLine($"[yellow]Showing first 20 of {result.Total} results[/]");
        }
    }
    else
    {
        AnsiConsole.MarkupLine("[yellow]No issues found matching your query.[/]");
    }
    
    return 0;
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
    return 1;
}

static void RegisterServices(IServiceCollection services, IConfiguration configuration)
{
    var useMocks = configuration.GetValue<bool>("USE_MOCKS", true);
    
    if (useMocks)
    {
        services.AddSingleton<ILlmClient, MockLlmClient>();
        services.AddSingleton<IJiraClient, MockJiraClient>();
    }
    
    services.AddSingleton<IQueryHandlerRegistry>(provider =>
    {
        var registry = new QueryHandlerRegistry();
        registry.RegisterHandler(new AssigneeHandler());
        registry.RegisterHandler(new ProjectHandler());
        registry.RegisterHandler(new StatusHandler());
        registry.RegisterHandler(new IssueTypeHandler());
        registry.RegisterHandler(new DateRangeHandler());
        registry.RegisterHandler(new SortHandler());
        return registry;
    });
    
    services.AddSingleton<IJqlBuilder, JqlBuilder>();
    services.AddSingleton<QueryEngine>();
    services.AddSingleton<QueryRunner>();
}

public class AppConfiguration
{
    public string? JiraBaseUrl { get; set; }
    public string? JiraEmail { get; set; }
    public string? JiraToken { get; set; }
    public string LlmProvider { get; set; } = "Mock";
    public string? LlmApiKey { get; set; }
    public bool UseMocks { get; set; } = true;
}

public class QueryRunner
{
    private readonly ILlmClient _llmClient;
    private readonly QueryEngine _queryEngine;
    private readonly IJiraClient _jiraClient;

    public QueryRunner(ILlmClient llmClient, QueryEngine queryEngine, IJiraClient jiraClient)
    {
        _llmClient = llmClient;
        _queryEngine = queryEngine;
        _jiraClient = jiraClient;
    }

    public async Task<QueryResult> RunQueryAsync(string naturalLanguageQuery)
    {
        var intent = await _llmClient.ParseNaturalLanguageAsync(naturalLanguageQuery);
        var jqlQuery = _queryEngine.BuildQuery(intent);
        var result = await _jiraClient.SearchAsync(jqlQuery);
        
        return result;
    }
}