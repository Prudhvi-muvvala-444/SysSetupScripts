using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add built-in health checks
builder.Services.AddHealthChecks()
    .AddCheck("Liveness", () => HealthCheckResult.Healthy(), tags: new[] { "liveness" })
    .AddCheck<DatabaseHealthCheck>("Readiness", tags: new[] { "readiness" });

var app = builder.Build();

// Map health check endpoints using built-in middleware
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("liveness")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("readiness")
});

app.MapGet("/", () => "Hello World!");

app.Run();

// Custom health check for database readiness
public class DatabaseHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        bool isDatabaseConnected = CheckDatabaseConnection();
        return Task.FromResult(isDatabaseConnected ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Database is not accessible"));
    }

    private bool CheckDatabaseConnection()
    {
        // Implement actual database connectivity check logic
        return true; // Return true if database is accessible, false otherwise
    }
}