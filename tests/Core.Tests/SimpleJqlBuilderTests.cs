using JQLBridge.Core.Domain;
using JQLBridge.Core.Jql;

namespace JQLBridge.Core.Tests;

public class SimpleJqlBuilderTests
{
    private readonly SimpleJqlBuilder _builder = new();

    [Fact]
    public void BuildQuery_BasicFilters_GeneratesCorrectJql()
    {
        // Arrange
        var intent = new QueryIntent(
            Filters: new QueryFilters(
                Project: "BANK",
                Assignee: "currentuser",
                Status: new[] { "Open", "In Progress" }
            )
        );

        // Act
        var result = _builder.BuildQuery(intent);

        // Assert
        Assert.Contains("project = \"BANK\"", result.Query);
        Assert.Contains("assignee = currentUser()", result.Query);
        Assert.Contains("status IN (\"Open\", \"In Progress\")", result.Query);
    }

    [Fact]
    public void BuildQuery_WithSearch_IncludesTextSearch()
    {
        // Arrange
        var intent = new QueryIntent(
            Search: "payment bug"
        );

        // Act
        var result = _builder.BuildQuery(intent);

        // Assert
        Assert.Contains("text ~ \"payment bug\"", result.Query);
    }

    [Fact]
    public void BuildQuery_WithDateRange_GeneratesCorrectDateFilter()
    {
        // Arrange
        var intent = new QueryIntent(
            Filters: new QueryFilters(
                Updated: new DateRange(LastDays: 7)
            )
        );

        // Act
        var result = _builder.BuildQuery(intent);

        // Assert
        Assert.Contains("updated >= -7d", result.Query);
    }

    [Fact]
    public void BuildQuery_WithLimit_SetsMaxResults()
    {
        // Arrange
        var intent = new QueryIntent(
            Limit: 25
        );

        // Act
        var result = _builder.BuildQuery(intent);

        // Assert
        Assert.Equal(25, result.MaxResults);
    }

    [Fact]
    public void BuildQuery_EmptyIntent_ReturnsEmptyQuery()
    {
        // Arrange
        var intent = new QueryIntent();

        // Act
        var result = _builder.BuildQuery(intent);

        // Assert
        Assert.Equal("", result.Query);
        Assert.Null(result.MaxResults);
    }
}