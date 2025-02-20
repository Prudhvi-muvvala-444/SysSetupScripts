using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthCheckTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Liveness_Check_Should_Return_Healthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_Check_Should_Return_Healthy_When_Database_Is_Accessible()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/ready");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}


using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Xunit;

public class DatabaseHealthCheckTests
{
    [Fact]
    public async Task DatabaseHealthCheck_Should_Return_Healthy_When_Database_Is_Accessible()
    {
        // Arrange
        var healthCheck = new DatabaseHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task DatabaseHealthCheck_Should_Return_Unhealthy_When_Database_Is_Not_Accessible()
    {
        // Arrange
        var healthCheck = new DatabaseHealthCheckWithFailure(); // Simulating database failure

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    // Mocking a failed database check by overriding the method
    private class DatabaseHealthCheckWithFailure : DatabaseHealthCheck
    {
        protected override bool CheckDatabaseConnection() => false;
    }
}