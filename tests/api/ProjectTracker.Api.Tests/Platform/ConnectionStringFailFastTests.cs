using System;
using System.Threading.Tasks;
using ProjectTracker.Api.Tests.TestInfrastructure;
using Xunit;

namespace ProjectTracker.Api.Tests.Platform;

/// <summary>
/// Given/When/Then source: Platform/gherkin-scenarios.md, section 3.
/// Verifies D-4.2: a missing connection string in a non-Development environment
/// must crash the process at startup, not silently fall back to a hardcoded
/// local connection string with a baked-in password.
/// </summary>
public class ConnectionStringFailFastTests
{
    // 3.1 Given the environment is Production and no connection string is configured,
    // When the application host starts,
    // Then startup fails rather than silently falling back to a hardcoded connection string.
    [Fact]
    public void Startup_fails_in_production_when_no_connection_string_is_configured()
    {
        using var factory = new PlatformWebApplicationFactory()
            .WithEnvironment("Production")
            // Isolate the connection-string concern from the unrelated
            // stub-authorization guard, which already (correctly) refuses to
            // run outside Development.
            .WithConfiguration("Board:AllowStubAuthorization", "true")
            .WithConfiguration("ConnectionStrings:Postgres", null)
            .WithConfiguration("DATABASE_URL", null);

        var exception = Record.Exception(() => factory.Services.GetType());

        Assert.NotNull(exception);
        Assert.Contains("connection", exception!.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
