using JQLBridge.Core.Domain;
using JQLBridge.Core.Llm;
using Microsoft.Extensions.Logging;
using Moq;

namespace JQLBridge.Core.Tests;

public class LlmIntegrationTests
{
    [Fact]
    public void LlmConfiguration_FromEnvironment_DefaultsToMock()
    {
        // Act
        var config = LlmConfiguration.FromEnvironment();

        // Assert
        Assert.Equal(LlmProvider.Mock, config.Provider);
        Assert.Null(config.ApiKey);
        Assert.Null(config.OpenAI);
        Assert.Null(config.Anthropic);
    }

    [Fact]
    public void LlmConfiguration_FromEnvironment_ParsesOpenAI()
    {
        // Arrange
        Environment.SetEnvironmentVariable("LLM_PROVIDER", "OpenAI");
        Environment.SetEnvironmentVariable("LLM_API_KEY", "test-key");
        Environment.SetEnvironmentVariable("OPENAI_MODEL", "gpt-4");
        Environment.SetEnvironmentVariable("OPENAI_TEMPERATURE", "0.5");
        Environment.SetEnvironmentVariable("OPENAI_MAX_TOKENS", "2000");

        try
        {
            // Act
            var config = LlmConfiguration.FromEnvironment();

            // Assert
            Assert.Equal(LlmProvider.OpenAI, config.Provider);
            Assert.Equal("test-key", config.ApiKey);
            Assert.NotNull(config.OpenAI);
            Assert.Equal("test-key", config.OpenAI.ApiKey);
            Assert.Equal("gpt-4", config.OpenAI.Model);
            Assert.Equal(0.5f, config.OpenAI.Temperature);
            Assert.Equal(2000, config.OpenAI.MaxTokens);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("LLM_PROVIDER", null);
            Environment.SetEnvironmentVariable("LLM_API_KEY", null);
            Environment.SetEnvironmentVariable("OPENAI_MODEL", null);
            Environment.SetEnvironmentVariable("OPENAI_TEMPERATURE", null);
            Environment.SetEnvironmentVariable("OPENAI_MAX_TOKENS", null);
        }
    }

    [Fact]
    public void LlmConfiguration_FromEnvironment_ParsesAnthropic()
    {
        // Arrange
        Environment.SetEnvironmentVariable("LLM_PROVIDER", "Anthropic");
        Environment.SetEnvironmentVariable("LLM_API_KEY", "test-key");
        Environment.SetEnvironmentVariable("ANTHROPIC_MODEL", "claude-3-opus");
        Environment.SetEnvironmentVariable("ANTHROPIC_TEMPERATURE", "0.3");
        Environment.SetEnvironmentVariable("ANTHROPIC_MAX_TOKENS", "1500");

        try
        {
            // Act
            var config = LlmConfiguration.FromEnvironment();

            // Assert
            Assert.Equal(LlmProvider.Anthropic, config.Provider);
            Assert.Equal("test-key", config.ApiKey);
            Assert.NotNull(config.Anthropic);
            Assert.Equal("test-key", config.Anthropic.ApiKey);
            Assert.Equal("claude-3-opus", config.Anthropic.Model);
            Assert.Equal(0.3m, config.Anthropic.Temperature);
            Assert.Equal(1500, config.Anthropic.MaxTokens);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("LLM_PROVIDER", null);
            Environment.SetEnvironmentVariable("LLM_API_KEY", null);
            Environment.SetEnvironmentVariable("ANTHROPIC_MODEL", null);
            Environment.SetEnvironmentVariable("ANTHROPIC_TEMPERATURE", null);
            Environment.SetEnvironmentVariable("ANTHROPIC_MAX_TOKENS", null);
        }
    }

    [Fact]
    public async Task ResilientLlmClient_RetriesOnTransientFailure()
    {
        // Arrange
        var innerClientMock = new Mock<ILlmClient>();
        var loggerMock = new Mock<ILogger<ResilientLlmClient>>();
        var retryConfig = new RetryConfiguration(MaxAttempts: 3);
        
        var client = new ResilientLlmClient(innerClientMock.Object, loggerMock.Object, retryConfig);
        
        var prompt = "test query";
        var expectedResult = new QueryIntent();
        
        // Setup to fail twice, then succeed
        var calls = 0;
        innerClientMock
            .Setup(x => x.ParseNaturalLanguageAsync(prompt, It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                calls++;
                if (calls <= 2)
                {
                    throw new HttpRequestException("Transient network error");
                }
                return Task.FromResult(expectedResult);
            });

        // Act
        var result = await client.ParseNaturalLanguageAsync(prompt);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(3, calls); // Should have been called 3 times (2 failures + 1 success)
    }

    [Fact]
    public async Task ResilientLlmClient_StopsRetryingAfterMaxAttempts()
    {
        // Arrange
        var innerClientMock = new Mock<ILlmClient>();
        var loggerMock = new Mock<ILogger<ResilientLlmClient>>();
        var retryConfig = new RetryConfiguration(MaxAttempts: 2);
        
        var client = new ResilientLlmClient(innerClientMock.Object, loggerMock.Object, retryConfig);
        
        var prompt = "test query";
        
        innerClientMock
            .Setup(x => x.ParseNaturalLanguageAsync(prompt, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Persistent network error"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => client.ParseNaturalLanguageAsync(prompt));
        
        // Verify it was called the maximum number of times
        innerClientMock.Verify(
            x => x.ParseNaturalLanguageAsync(prompt, It.IsAny<CancellationToken>()), 
            Times.Exactly(2));
    }

    [Fact]
    public async Task ResilientLlmClient_DoesNotRetryOnNonRetryableException()
    {
        // Arrange
        var innerClientMock = new Mock<ILlmClient>();
        var loggerMock = new Mock<ILogger<ResilientLlmClient>>();
        var retryConfig = new RetryConfiguration(MaxAttempts: 3);
        
        var client = new ResilientLlmClient(innerClientMock.Object, loggerMock.Object, retryConfig);
        
        var prompt = "test query";
        var nonRetryableException = new ArgumentException("Invalid argument");
        
        innerClientMock
            .Setup(x => x.ParseNaturalLanguageAsync(prompt, It.IsAny<CancellationToken>()))
            .ThrowsAsync(nonRetryableException);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => client.ParseNaturalLanguageAsync(prompt));
        
        // Verify it was only called once (no retries)
        innerClientMock.Verify(
            x => x.ParseNaturalLanguageAsync(prompt, It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}