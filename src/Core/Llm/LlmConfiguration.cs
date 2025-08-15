namespace JQLBridge.Core.Llm;

public enum LlmProvider
{
    Mock,
    OpenAI,
    Anthropic
}

public record LlmConfiguration(
    LlmProvider Provider = LlmProvider.Mock,
    string? ApiKey = null,
    OpenAIConfiguration? OpenAI = null,
    AnthropicConfiguration? Anthropic = null)
{
    public static LlmConfiguration FromEnvironment()
    {
        var provider = Enum.TryParse<LlmProvider>(
            Environment.GetEnvironmentVariable("LLM_PROVIDER"), 
            ignoreCase: true, 
            out var parsedProvider) ? parsedProvider : LlmProvider.Mock;

        var apiKey = Environment.GetEnvironmentVariable("LLM_API_KEY");

        var openAIConfig = provider == LlmProvider.OpenAI && !string.IsNullOrEmpty(apiKey) 
            ? new OpenAIConfiguration(
                ApiKey: apiKey,
                Model: Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o",
                Temperature: ParseFloat("OPENAI_TEMPERATURE", 0.1f),
                MaxTokens: ParseInt("OPENAI_MAX_TOKENS", 1000))
            : null;

        var anthropicConfig = provider == LlmProvider.Anthropic && !string.IsNullOrEmpty(apiKey)
            ? new AnthropicConfiguration(
                ApiKey: apiKey,
                Model: Environment.GetEnvironmentVariable("ANTHROPIC_MODEL") ?? "claude-3-5-sonnet-20241022",
                Temperature: ParseDecimal("ANTHROPIC_TEMPERATURE", 0.1m),
                MaxTokens: ParseInt("ANTHROPIC_MAX_TOKENS", 1000))
            : null;

        return new LlmConfiguration(
            Provider: provider,
            ApiKey: apiKey,
            OpenAI: openAIConfig,
            Anthropic: anthropicConfig);
    }

    private static float ParseFloat(string envVar, float defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(envVar);
        return float.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static decimal ParseDecimal(string envVar, decimal defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(envVar);
        return decimal.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static int ParseInt(string envVar, int defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(envVar);
        return int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}