using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ProjectTracker.Api.Tests.TestInfrastructure;
using Xunit;

namespace ProjectTracker.Api.Tests.Platform;

/// <summary>
/// Given/When/Then source: Platform/gherkin-scenarios.md, section 2.
/// Verifies D-4.1: CORS must be an explicit, per-environment allow-list with
/// credentials support, not AllowAnyOrigin().
/// </summary>
public class CorsPolicyTests
{
    // 2.1 Given Cors:AllowedOrigins is configured to ["https://staging.example.com"],
    // When a cross-origin request is sent with Origin: https://staging.example.com,
    // Then the response echoes the literal origin (not "*") and allows credentials.
    [Fact]
    public async Task Allows_credentialed_requests_from_a_configured_allowed_origin()
    {
        const string allowedOrigin = "https://staging.example.com";

        await using var factory = new PlatformWebApplicationFactory()
            .WithConfiguration("Cors:AllowedOrigins:0", allowedOrigin);

        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/healthz/live");
        request.Headers.Add("Origin", allowedOrigin);

        var response = await client.SendAsync(request);

        var allowOriginHeader = response.Headers
            .FirstOrDefault(h => h.Key == "Access-Control-Allow-Origin")
            .Value?.FirstOrDefault();
        var allowCredentialsHeader = response.Headers
            .FirstOrDefault(h => h.Key == "Access-Control-Allow-Credentials")
            .Value?.FirstOrDefault();

        Assert.Equal(allowedOrigin, allowOriginHeader);
        Assert.Equal("true", allowCredentialsHeader);
    }

    // 2.2 Given Cors:AllowedOrigins is configured to ["https://staging.example.com"],
    // When a cross-origin request is sent with Origin: https://evil.example.com,
    // Then the response does not include an Access-Control-Allow-Origin header.
    [Fact]
    public async Task Does_not_allow_a_disallowed_origin()
    {
        await using var factory = new PlatformWebApplicationFactory()
            .WithConfiguration("Cors:AllowedOrigins:0", "https://staging.example.com");

        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/healthz/live");
        request.Headers.Add("Origin", "https://evil.example.com");

        var response = await client.SendAsync(request);

        var allowOriginHeader = response.Headers
            .FirstOrDefault(h => h.Key == "Access-Control-Allow-Origin")
            .Value?.FirstOrDefault();

        Assert.Null(allowOriginHeader);
    }
}
