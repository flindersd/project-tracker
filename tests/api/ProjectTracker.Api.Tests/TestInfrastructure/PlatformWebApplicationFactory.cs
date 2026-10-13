using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ProjectTracker.Api.Tests.TestInfrastructure;

/// <summary>
/// A <see cref="WebApplicationFactory{TEntryPoint}"/> that lets each test override
/// configuration (connection strings, CORS allow-list, environment name) without
/// touching real environment variables, so platform-foundation behaviour can be
/// exercised in isolation.
/// </summary>
public sealed class PlatformWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Dictionary<string, string?> configurationOverrides = new();
    private string environmentName = "Development";

    public PlatformWebApplicationFactory WithEnvironment(string name)
    {
        environmentName = name;
        return this;
    }

    public PlatformWebApplicationFactory WithConfiguration(string key, string? value)
    {
        configurationOverrides[key] = value;
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environmentName);
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(configurationOverrides);
        });
    }
}
