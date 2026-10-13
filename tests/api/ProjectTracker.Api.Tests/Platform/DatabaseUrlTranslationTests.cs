using System.Net;
using System.Threading.Tasks;
using ProjectTracker.Api.Tests.TestInfrastructure;
using Testcontainers.PostgreSql;
using Xunit;

namespace ProjectTracker.Api.Tests.Platform;

/// <summary>
/// Given/When/Then source: Platform/gherkin-scenarios.md, section 4.
/// Verifies D-4.3: DATABASE_URL arrives from Railway in libpq URI form
/// (postgresql://user:pass@host:port/db) and must be translated before being
/// handed to Npgsql, which does not understand the URI form natively.
/// </summary>
public class DatabaseUrlTranslationTests
{
    // 4.1 Given DATABASE_URL is set to a libpq URI pointing at a real reachable
    // PostgreSQL instance,
    // When a request that requires database access is made,
    // Then the request succeeds instead of failing with a connection-string parse error.
    [Fact]
    public async Task A_libpq_style_DATABASE_URL_is_usable_for_real_requests()
    {
        await using var container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("projecttracker")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
        await container.StartAsync();

        var libpqUri =
            $"postgresql://postgres:postgres@{container.Hostname}:{container.GetMappedPublicPort(5432)}/projecttracker";

        await using var factory = new PlatformWebApplicationFactory()
            .WithConfiguration("DATABASE_URL", libpqUri);

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/boards");

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
