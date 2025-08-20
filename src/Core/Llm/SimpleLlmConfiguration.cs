namespace JQLBridge.Core.Llm;

public enum SimpleLlmProvider
{
    Mock,
    OpenAI
}

public record SimpleLlmConfiguration(
    SimpleLlmProvider Provider,
    string? ApiKey = null,
    string Model = "gpt-4")
{
    public static SimpleLlmConfiguration FromEnvironment()
    {
        var provider = Environment.GetEnvironmentVariable("LLM_PROVIDER")?.ToLowerInvariant() switch
        {
            "openai" => SimpleLlmProvider.OpenAI,
            _ => SimpleLlmProvider.Mock
        };

        var apiKey = Environment.GetEnvironmentVariable("LLM_API_KEY");
        var model = Environment.GetEnvironmentVariable("LLM_MODEL") ?? "gpt-4";

        return new SimpleLlmConfiguration(provider, apiKey, model);
    }
}