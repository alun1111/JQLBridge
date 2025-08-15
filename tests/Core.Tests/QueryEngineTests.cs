using JQLBridge.Core.Domain;
using JQLBridge.Core.QueryEngine;
using JQLBridge.Core.QueryEngine.Handlers;

namespace JQLBridge.Core.Tests;

public class QueryEngineTests
{
    [Fact]
    public void AssigneeHandler_ShouldGenerateCurrentUserJql()
    {
        var handler = new AssigneeHandler();
        var intent = new QueryIntent(
            Filters: new QueryFilters(Assignee: "currentUser"));

        var result = handler.Handle(intent);

        Assert.Equal("assignee = currentUser()", result.WhereClause);
    }

    [Fact]
    public void StatusHandler_ShouldGenerateSingleStatusJql()
    {
        var handler = new StatusHandler();
        var intent = new QueryIntent(
            Filters: new QueryFilters(Status: new[] { "Open" }));

        var result = handler.Handle(intent);

        Assert.Equal("status = \"Open\"", result.WhereClause);
    }

    [Fact]
    public void StatusHandler_ShouldGenerateMultipleStatusJql()
    {
        var handler = new StatusHandler();
        var intent = new QueryIntent(
            Filters: new QueryFilters(Status: new[] { "Open", "In Progress" }));

        var result = handler.Handle(intent);

        Assert.Equal("status IN (\"Open\", \"In Progress\")", result.WhereClause);
    }

    [Fact]
    public void DateRangeHandler_ShouldGenerateLastDaysJql()
    {
        var handler = new DateRangeHandler();
        var intent = new QueryIntent(
            Filters: new QueryFilters(Updated: new DateRange(LastDays: 7)));

        var result = handler.Handle(intent);

        Assert.Equal("updated >= -7d", result.WhereClause);
    }

    [Fact]
    public void JqlBuilder_ShouldCombineFragments()
    {
        var builder = new JqlBuilder();
        var fragment = new JqlFragment(
            WhereClause: "assignee = currentUser() AND status = \"Open\"",
            OrderByClause: "updated DESC");
        var intent = new QueryIntent();

        var result = builder.BuildQuery(fragment, intent);

        Assert.Equal("assignee = currentUser() AND status = \"Open\" ORDER BY updated DESC", result.Query);
    }
}