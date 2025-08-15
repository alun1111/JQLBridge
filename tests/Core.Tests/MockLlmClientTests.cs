using JQLBridge.Core.Domain;
using JQLBridge.Core.Llm;

namespace JQLBridge.Core.Tests;

public class MockLlmClientTests
{
    [Fact]
    public async Task ParseNaturalLanguageAsync_ShouldRecognizeBugsAssignedToMe()
    {
        var client = new MockLlmClient();

        var result = await client.ParseNaturalLanguageAsync("bugs assigned to me");

        Assert.NotNull(result.Filters);
        Assert.Equal("currentUser", result.Filters.Assignee);
        Assert.Contains("Bug", result.Filters.IssueTypes ?? Array.Empty<string>());
    }

    [Fact]
    public async Task ParseNaturalLanguageAsync_ShouldRecognizeProjectFilter()
    {
        var client = new MockLlmClient();

        var result = await client.ParseNaturalLanguageAsync("stories in BANK project");

        Assert.NotNull(result.Filters);
        Assert.Equal("BANK", result.Filters.Project);
        Assert.Contains("Story", result.Filters.IssueTypes ?? Array.Empty<string>());
    }

    [Fact]
    public async Task ParseNaturalLanguageAsync_ShouldRecognizeDateRange()
    {
        var client = new MockLlmClient();

        var result = await client.ParseNaturalLanguageAsync("issues updated last 7 days");

        Assert.NotNull(result.Filters?.Updated);
        Assert.Equal(7, result.Filters.Updated.LastDays);
    }

    [Fact]
    public async Task ParseNaturalLanguageAsync_ShouldRecognizeHighPriority()
    {
        var client = new MockLlmClient();

        var result = await client.ParseNaturalLanguageAsync("high priority bugs");

        Assert.NotNull(result.Filters);
        Assert.Contains("High", result.Filters.Priorities ?? Array.Empty<string>());
        Assert.Contains("Bug", result.Filters.IssueTypes ?? Array.Empty<string>());
    }
}