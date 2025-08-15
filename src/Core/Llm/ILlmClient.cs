using JQLBridge.Core.Domain;

namespace JQLBridge.Core.Llm;

public interface ILlmClient
{
    Task<QueryIntent> ParseNaturalLanguageAsync(string prompt, CancellationToken cancellationToken = default);
}

public record LlmRequest(
    string SystemPrompt,
    string UserPrompt,
    double Temperature = 0.1);

public record LlmResponse(
    QueryIntent Intent,
    string? RawResponse = null);