using System.Reflection;
using JQLBridge.Core.Domain;
using JQLBridge.Core.Jira;
using JQLBridge.Core.Llm;
using JQLBridge.Core.Jql;
using JQLBridge.Core.Processing;
using JQLBridge.Core.Output;
using OpenAI.Chat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    AnsiConsole.MarkupLine("[red]Usage: JQLBridge.Cli \"<natural language query>\" [options][/]");
    AnsiConsole.MarkupLine("[yellow]Example: JQLBridge.Cli \"show bugs assigned to me updated last 7 days\"[/]");
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[blue]Post-Processing Options:[/]");
    AnsiConsole.MarkupLine("  --format <format>      Output format: table, json, summary (default: table)");
    AnsiConsole.MarkupLine("  --group-by <field>     Group results by field (status, assignee, priority, etc.)");
    AnsiConsole.MarkupLine("  --calculate <calc>     Add calculations (age, velocity, avgAge, statusDistribution)");
    AnsiConsole.MarkupLine("  --aggregate <agg>      Add aggregations (count, avg_age, status_counts, etc.)");
    AnsiConsole.MarkupLine("  --debug                Show step-by-step processing output");
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[blue]Configuration:[/]");
    AnsiConsole.MarkupLine("  Mock mode:  [green]USE_MOCKS=true[/] (default)");
    AnsiConsole.MarkupLine("  Real JIRA:  [green]USE_MOCKS=false JIRA_BASE_URL=https://yourcompany.atlassian.net JIRA_EMAIL=you@company.com JIRA_TOKEN=your_api_token[/]");
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[blue]LLM Configuration:[/]");
    AnsiConsole.MarkupLine("  Mock LLM:   [green]LLM_PROVIDER=Mock[/] (default)");
    AnsiConsole.MarkupLine("  OpenAI:     [green]LLM_PROVIDER=OpenAI LLM_API_KEY=your_openai_key[/]");
    AnsiConsole.MarkupLine("  Anthropic:  [green]LLM_PROVIDER=Anthropic LLM_API_KEY=your_anthropic_key[/]");
    return 1;
}

var (query, processingOptions, debug) = ParseArgs(args);

try
{
    var result = await queryRunner.RunQueryAsync(query, processingOptions, debug);
    var formatterRegistry = app.Services.GetRequiredService<OutputFormatterRegistry>();
    
    var format = processingOptions?.OutputFormat ?? "table";
    var formatter = formatterRegistry.GetFormatter(format) ?? formatterRegistry.GetFormatter("table")!;
    
    var output = await formatter.FormatAsync(result, result.ProcessedData != null ? 
        new ProcessingResult 
        { 
            Issues = result.Issues,
            Groups = result.ProcessedData.Groups,
            Calculations = result.ProcessedData.Calculations.ToDictionary(k => k.Key, k => k.Value),
            Metadata = result.ProcessedData.Metadata.ToDictionary(k => k.Key, k => k.Value)
        } : null);
    
    if (!debug || format != "table")
    {
        AnsiConsole.WriteLine(output);
    }
    else
    {
        Console.WriteLine();
        Console.WriteLine("📋 Final Output:");
        AnsiConsole.WriteLine(output);
    }
    
    return 0;
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
    return 1;
}

static (string query, ProcessingOptions? options, bool debug) ParseArgs(string[] args)
{
    var queryParts = new List<string>();
    var groupBy = new List<string>();
    var calculate = new List<string>();
    var aggregate = new List<string>();
    string? format = null;
    bool debug = false;

    for (int i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        
        if (arg == "--format" && i + 1 < args.Length)
        {
            format = args[++i];
        }
        else if (arg == "--group-by" && i + 1 < args.Length)
        {
            groupBy.Add(args[++i]);
        }
        else if (arg == "--calculate" && i + 1 < args.Length)
        {
            calculate.Add(args[++i]);
        }
        else if (arg == "--aggregate" && i + 1 < args.Length)
        {
            aggregate.Add(args[++i]);
        }
        else if (arg == "--debug")
        {
            debug = true;
        }
        else if (!arg.StartsWith("--"))
        {
            queryParts.Add(arg);
        }
    }

    var query = string.Join(" ", queryParts);
    
    var options = (groupBy.Any() || calculate.Any() || aggregate.Any() || format != null) ? 
        new ProcessingOptions
        {
            GroupBy = groupBy.Any() ? groupBy : null,
            Calculate = calculate.Any() ? calculate : null,
            Aggregate = aggregate.Any() ? aggregate : null,
            OutputFormat = format,
            CustomOptions = debug ? new Dictionary<string, object> { ["debug"] = true } : new Dictionary<string, object>()
        } : null;

    return (query, options, debug);
}

static void RegisterServices(IServiceCollection services, IConfiguration configuration)
{
    var useMocks = configuration.GetValue<bool>("USE_MOCKS", true);
    
    // Configure Jira client
    if (useMocks)
    {
        services.AddSingleton<IJiraClient, MockJiraClient>();
    }
    else
    {
        var jiraConfig = new JiraConfiguration(
            BaseUrl: configuration["JIRA_BASE_URL"],
            Email: configuration["JIRA_EMAIL"],
            ApiToken: configuration["JIRA_TOKEN"]
        );
        
        services.AddSingleton(jiraConfig);
        services.AddHttpClient<JiraApiClient>();
        services.AddSingleton<IJiraClient, JiraApiClient>();
    }
    
    // Configure LLM client
    var llmConfig = SimpleLlmConfiguration.FromEnvironment();
    
    if (useMocks || llmConfig.Provider == SimpleLlmProvider.Mock)
    {
        services.AddSingleton<ILlmClient, MockLlmClient>();
    }
    else
    {
        switch (llmConfig.Provider)
        {
            case SimpleLlmProvider.OpenAI:
                if (string.IsNullOrEmpty(llmConfig.ApiKey))
                {
                    throw new InvalidOperationException("OpenAI API key is missing. Set LLM_API_KEY environment variable.");
                }
                
                services.AddSingleton<ChatClient>(provider => 
                    new ChatClient(model: llmConfig.Model, apiKey: llmConfig.ApiKey));
                services.AddSingleton<ILlmClient, OpenAILlmClient>();
                break;
                
            default:
                services.AddSingleton<ILlmClient, MockLlmClient>();
                break;
        }
    }
    
    services.AddSingleton<ISimpleJqlBuilder, SimpleJqlBuilder>();
    
    // Post-processing services
    services.AddSingleton<IFieldAccessor, FieldAccessor>();
    services.AddSingleton<IUnifiedProcessor, UnifiedProcessor>();
    
    // Register output formatters
    services.AddSingleton<OutputFormatterRegistry>(provider =>
    {
        var registry = new OutputFormatterRegistry();
        registry.RegisterFormatter(new TableFormatter());
        registry.RegisterFormatter(new JsonFormatter());
        registry.RegisterFormatter(new SummaryFormatter());
        return registry;
    });
    
    services.AddSingleton<IDebugOutput>(provider => new NullDebugOutput());
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
    private readonly ISimpleJqlBuilder _jqlBuilder;
    private readonly IJiraClient _jiraClient;
    private readonly IUnifiedProcessor _processor;

    public QueryRunner(ILlmClient llmClient, ISimpleJqlBuilder jqlBuilder, IJiraClient jiraClient, IUnifiedProcessor processor)
    {
        _llmClient = llmClient;
        _jqlBuilder = jqlBuilder;
        _jiraClient = jiraClient;
        _processor = processor;
    }

    public async Task<QueryResult> RunQueryAsync(string naturalLanguageQuery, ProcessingOptions? processingOptions = null, bool debug = false)
    {
        IDebugOutput debugOutput = debug ? new ConsoleDebugOutput(true) : new NullDebugOutput();
        
        debugOutput.WriteStep("🚀 Starting query processing", naturalLanguageQuery);
        
        debugOutput.WriteStep("🧠 Parsing natural language query with LLM");
        var intent = await (_llmClient is MockLlmClient mockClient 
            ? mockClient.ParseNaturalLanguageAsync(naturalLanguageQuery, debug)
            : _llmClient.ParseNaturalLanguageAsync(naturalLanguageQuery));
        debugOutput.WriteSubStep("Parsed intent", new 
        { 
            Filters = intent.Filters,
            Search = intent.Search,
            Sort = intent.Sort?.Count,
            Limit = intent.Limit,
            // Aggregations moved to post-processing
        });

        debugOutput.WriteStep("🔧 Building JQL query from intent");
        var jqlQuery = _jqlBuilder.BuildQuery(intent);
        debugOutput.WriteSubStep("Generated JQL", jqlQuery.Query);
        if (jqlQuery.MaxResults.HasValue)
            debugOutput.WriteSubStep("Max results limit", jqlQuery.MaxResults.Value);

        debugOutput.WriteStep("🔍 Executing JIRA search");
        var result = await _jiraClient.SearchAsync(jqlQuery);
        debugOutput.WriteSubStep("Found issues", result.Issues.Count);
        debugOutput.WriteSubStep("Total available", result.Total);
        
        if (processingOptions != null)
        {
            debugOutput.WriteStep("⚙️ Starting post-processing pipeline");
            
            var context = new ProcessingContext(
                Issues: result.Issues,
                OriginalIntent: intent,
                Options: processingOptions
            );
            
            if (processingOptions.GroupBy?.Any() == true)
                debugOutput.WriteSubStep("Group by fields", processingOptions.GroupBy);
            if (processingOptions.Calculate?.Any() == true)
                debugOutput.WriteSubStep("Calculations", processingOptions.Calculate);
            if (processingOptions.Aggregate?.Any() == true)
                debugOutput.WriteSubStep("Aggregations", processingOptions.Aggregate);
            
            var processed = await _processor.ProcessAsync(context);
            
            debugOutput.WriteSubStep("Processing complete", new
            {
                Groups = processed.Groups.Count,
                Calculations = processed.Calculations.Count,
                Aggregations = processed.Aggregations.Count
            });
            
            result = result with 
            { 
                ProcessedData = new ProcessedData(
                    Groups: processed.Groups,
                    Calculations: processed.Calculations,
                    Metadata: processed.Metadata
                )
            };
        }

        debugOutput.WriteStep("✅ Query processing complete");
        
        return result;
    }
}