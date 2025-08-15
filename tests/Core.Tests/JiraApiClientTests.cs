using System.Net;
using System.Text.Json;
using JQLBridge.Core.Domain;
using JQLBridge.Core.Jira;
using JQLBridge.Core.QueryEngine;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace JQLBridge.Core.Tests;

public class JiraApiClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly JiraConfiguration _configuration;
    private readonly Mock<ILogger<JiraApiClient>> _loggerMock;
    private readonly JiraApiClient _jiraApiClient;

    public JiraApiClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _configuration = new JiraConfiguration(
            BaseUrl: "https://test.atlassian.net",
            Email: "test@example.com",
            ApiToken: "test-token");
        _loggerMock = new Mock<ILogger<JiraApiClient>>();
        _jiraApiClient = new JiraApiClient(_httpClient, _configuration, _loggerMock.Object);
    }

    [Fact]
    public async Task SearchAsync_ValidResponse_ReturnsQueryResult()
    {
        // Arrange
        var mockResponse = new
        {
            issues = new[]
            {
                new
                {
                    id = "1",
                    key = "TEST-123",
                    fields = new
                    {
                        summary = "Test Issue",
                        description = "Test Description",
                        status = new { name = "Open" },
                        priority = new { name = "High" },
                        issuetype = new { name = "Bug" },
                        assignee = new
                        {
                            accountId = "test-user",
                            displayName = "Test User",
                            emailAddress = "test@example.com"
                        },
                        reporter = new
                        {
                            accountId = "reporter-user",
                            displayName = "Reporter User", 
                            emailAddress = "reporter@example.com"
                        },
                        project = new { key = "TEST", name = "Test Project" },
                        created = "2023-08-15T10:00:00.000+0000",
                        updated = "2023-08-15T12:00:00.000+0000",
                        resolutiondate = (string?)null,
                        labels = new[] { "test", "bug" },
                        components = new[] { new { name = "frontend" } },
                        fixVersions = new[] { new { name = "1.0.0" } },
                        resolution = (object?)null
                    }
                }
            },
            total = 1,
            maxResults = 50,
            startAt = 0
        };

        var responseJson = JsonSerializer.Serialize(mockResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var query = new JqlQuery("project = TEST", 50);

        // Act
        var result = await _jiraApiClient.SearchAsync(query);

        // Assert
        Assert.Single(result.Issues);
        Assert.Equal("TEST-123", result.Issues[0].Key);
        Assert.Equal("Test Issue", result.Issues[0].Summary);
        Assert.Equal(IssueStatus.Open, result.Issues[0].Status);
        Assert.Equal(IssuePriority.High, result.Issues[0].Priority);
        Assert.Equal(IssueType.Bug, result.Issues[0].Type);
        Assert.Equal("test-user", result.Issues[0].Assignee?.Id);
        Assert.Equal(1, result.Total);
        Assert.Equal("project = TEST", result.GeneratedJql);
    }

    [Fact]
    public async Task SearchAsync_HttpError_ThrowsJiraApiException()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var query = new JqlQuery("project = TEST", 50);

        // Act & Assert
        await Assert.ThrowsAsync<JiraApiException>(() => _jiraApiClient.SearchAsync(query));
    }

    [Fact]
    public async Task GetIssueAsync_IssueNotFound_ReturnsNull()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound);
        
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _jiraApiClient.GetIssueAsync("TEST-404");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProjectsAsync_ValidResponse_ReturnsProjectKeys()
    {
        // Arrange
        var mockResponse = new[]
        {
            new { key = "PROJ1", name = "Project 1" },
            new { key = "PROJ2", name = "Project 2" }
        };

        var responseJson = JsonSerializer.Serialize(mockResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _jiraApiClient.GetProjectsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("PROJ1", result);
        Assert.Contains("PROJ2", result);
    }
}