using JQLBridge.Core.Domain;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace JQLBridge.Core.Llm;

public class ResilientLlmClient : ILlmClient
{
    private readonly ILlmClient _innerClient;
    private readonly ILogger<ResilientLlmClient> _logger;
    private readonly RetryConfiguration _retryConfig;

    public ResilientLlmClient(ILlmClient innerClient, ILogger<ResilientLlmClient> logger, RetryConfiguration? retryConfig = null)
    {
        _innerClient = innerClient;
        _logger = logger;
        _retryConfig = retryConfig ?? new RetryConfiguration();
    }

    public async Task<QueryIntent> ParseNaturalLanguageAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var attempt = 1;
        var maxAttempts = _retryConfig.MaxAttempts;

        while (attempt <= maxAttempts)
        {
            try
            {
                return await _innerClient.ParseNaturalLanguageAsync(prompt, cancellationToken);
            }
            catch (LlmException ex) when (attempt < maxAttempts && IsRetryableException(ex))
            {
                _logger.LogWarning(ex, "LLM request failed on attempt {Attempt}/{MaxAttempts} for query: {Query}", 
                    attempt, maxAttempts, prompt);

                var delay = CalculateDelay(attempt);
                await Task.Delay(delay, cancellationToken);
                attempt++;
            }
            catch (Exception ex) when (attempt < maxAttempts && IsTransientException(ex))
            {
                _logger.LogWarning(ex, "Transient error on attempt {Attempt}/{MaxAttempts} for query: {Query}", 
                    attempt, maxAttempts, prompt);

                var delay = CalculateDelay(attempt);
                await Task.Delay(delay, cancellationToken);
                attempt++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse query after {Attempts} attempts: {Query}", 
                    attempt, prompt);
                throw;
            }
        }

        throw new LlmException($"Failed to parse query after {maxAttempts} attempts");
    }

    private bool IsRetryableException(LlmException ex)
    {
        // Retry on rate limiting, temporary service issues, network problems
        return ex.InnerException switch
        {
            HttpRequestException => true,
            TaskCanceledException => true,
            SocketException => true,
            _ when ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) => true,
            _ when ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) => true,
            _ when ex.Message.Contains("service unavailable", StringComparison.OrdinalIgnoreCase) => true,
            _ => false
        };
    }

    private static bool IsTransientException(Exception ex)
    {
        return ex switch
        {
            HttpRequestException => true,
            TaskCanceledException when !ex.Message.Contains("cancellation", StringComparison.OrdinalIgnoreCase) => true,
            SocketException => true,
            _ => false
        };
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        // Exponential backoff with jitter
        var baseDelay = _retryConfig.BaseDelay.TotalMilliseconds;
        var exponentialDelay = baseDelay * Math.Pow(_retryConfig.BackoffMultiplier, attempt - 1);
        
        // Add jitter to prevent thundering herd
        var jitter = Random.Shared.NextDouble() * 0.1; // 0-10% jitter
        var totalDelay = exponentialDelay * (1 + jitter);
        
        return TimeSpan.FromMilliseconds(Math.Min(totalDelay, _retryConfig.MaxDelay.TotalMilliseconds));
    }
}

public record RetryConfiguration(
    int MaxAttempts = 3,
    double BackoffMultiplier = 2.0)
{
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromMinutes(1);
}