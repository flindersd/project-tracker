using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectTracker.Api.Modules.Board.Persistence;
using ProjectTracker.Api.Tests.TestInfrastructure;
using Testcontainers.PostgreSql;
using Xunit;

namespace ProjectTracker.Api.Tests.Platform;

/// <summary>
/// Given/When/Then source: Platform/gherkin-scenarios.md, section 1.
/// Verifies D-3: /healthz/ready must perform a real database round trip and a
/// pending-migration check, not return a hardcoded { status: "ok" }.
/// </summary>
public class HealthEndpointTests
{
    // 1.1 Given the configured PostgreSQL database is unreachable,
    // When GET /healthz/ready is called,
    // Then the response status is 503 and the body names the failing check.
    [Fact]
    public async Task Ready_returns_503_when_database_is_unreachable()
    {
        await using var factory = new PlatformWebApplicationFactory()
            .WithConfiguration("ConnectionStrings:Postgres", "Host=127.0.0.1;Port=1;Database=doesnotexist;Username=postgres;Password=postgres;Timeout=1");

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/healthz/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("postgres", body, StringComparison.OrdinalIgnoreCase);
    }

    // 1.2 Given the configured PostgreSQL database has pending migrations for BoardDbContext,
    // When GET /healthz/ready is called,
    // Then the response status is 503.
    [Fact]
    public async Task Ready_returns_503_when_migrations_are_pending()
    {
        await using var container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("projecttracker")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
        await container.StartAsync();

        // Deliberately do NOT run migrations against this fresh database.
        await using var factory = new PlatformWebApplicationFactory()
            .WithConfiguration("ConnectionStrings:Postgres", container.GetConnectionString());

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/healthz/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    // 1.3 Given the database is reachable and all BoardDbContext migrations are applied,
    // When GET /healthz/ready is called,
    // Then the response is 200 with a structured per-check breakdown, not a bare constant.
    [Fact]
    public async Task Ready_returns_200_with_structured_checks_when_database_is_healthy()
    {
        await using var container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("projecttracker")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
        await container.StartAsync();

        await using var factory = new PlatformWebApplicationFactory()
            .WithConfiguration("ConnectionStrings:Postgres", container.GetConnectionString());

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BoardDbContext>();
            await db.Database.MigrateAsync();
        }

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/healthz/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(payload.TryGetProperty("checks", out var checks), "expected a structured 'checks' breakdown, not a bare status constant");
        Assert.True(checks.GetArrayLength() > 0);
    }
}
