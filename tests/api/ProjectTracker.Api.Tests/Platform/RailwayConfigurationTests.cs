using System;
using System.IO;
using System.Linq;
using Xunit;

namespace ProjectTracker.Api.Tests.Platform;

/// <summary>
/// Given/When/Then source: Platform/gherkin-scenarios.md, section 6.
/// Verifies D-6: railway.toml's startCommand must resolve against the Nixpacks
/// publish output, and healthcheckPath must point at the real readiness check
/// so Railway's own rollout gate uses it.
/// </summary>
public class RailwayConfigurationTests
{
    // 6.1 Given src/api/railway.toml,
    // When its [deploy] section is inspected,
    // Then startCommand references a fully-qualified path to the published DLL.
    [Fact]
    public void StartCommand_references_a_fully_qualified_dll_path()
    {
        var content = ReadRailwayToml();

        var startCommandLine = content
            .Split('\n')
            .FirstOrDefault(line => line.TrimStart().StartsWith("startCommand"));

        Assert.False(string.IsNullOrEmpty(startCommandLine), "startCommand is not set in railway.toml");

        // A bare "dotnet ProjectTracker.Api.dll" has no path and will not resolve
        // against the Nixpacks publish output directory.
        Assert.DoesNotContain("\"dotnet ProjectTracker.Api.dll\"", startCommandLine);
        Assert.Contains("/", startCommandLine);
    }

    // 6.2 Given src/api/railway.toml,
    // When its [deploy] section is inspected,
    // Then healthcheckPath is set to /healthz/ready.
    [Fact]
    public void HealthcheckPath_points_at_the_real_readiness_endpoint()
    {
        var content = ReadRailwayToml();

        Assert.Contains("healthcheckPath", content);
        Assert.Contains("/healthz/ready", content);
    }

    private static string ReadRailwayToml()
    {
        var directory = AppContext.BaseDirectory;
        const string relativePath = "src/api/railway.toml";

        while (directory is not null && !File.Exists(Path.Combine(directory, relativePath)))
        {
            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.True(directory is not null, $"could not locate repo root containing {relativePath}");
        return File.ReadAllText(Path.Combine(directory!, relativePath));
    }
}
