using System;
using System.IO;
using System.Linq;
using Xunit;

namespace ProjectTracker.Api.Tests.Platform;

/// <summary>
/// Given/When/Then source: Platform/gherkin-scenarios.md, section 6.
/// Verifies D-6: Railway must build and start this service via an explicit
/// Dockerfile (builder = "DOCKERFILE"), not Nixpacks/Railpack auto-detection,
/// because both auto-detecting builders misbuilt this monorepo by defaulting
/// to ProjectTracker.Api.sln, which references a test project outside the
/// Root-Directory-scoped build context. The Dockerfile's ENTRYPOINT now owns
/// starting the process (railway.toml no longer has a [deploy] startCommand),
/// and must reference the real published DLL and bind to Railway's injected
/// $PORT rather than a bare/broken or hardcoded-port command. healthcheckPath
/// must still point at the real readiness check so Railway's own rollout gate
/// uses it.
/// </summary>
public class RailwayConfigurationTests
{
    // 6.1 Given src/api/railway.toml,
    // When its [build] section is inspected,
    // Then builder is set to "DOCKERFILE", removing Nixpacks/Railpack
    // auto-detection ambiguity.
    [Fact]
    public void Builder_is_set_to_dockerfile()
    {
        var content = ReadRailwayToml();

        Assert.Contains("builder", content);
        Assert.Contains("\"DOCKERFILE\"", content);
    }

    // 6.2 Given the Root-Directory-scoped build context for this service,
    // When the build context is inspected,
    // Then src/api/Dockerfile exists for Railway to build from.
    [Fact]
    public void Dockerfile_exists_in_the_build_context()
    {
        var path = FindRepoFile("src/api/Dockerfile");

        Assert.True(File.Exists(path), "src/api/Dockerfile does not exist");
    }

    // 6.3 Given src/api/Dockerfile,
    // When its ENTRYPOINT (or CMD) is inspected,
    // Then it references the actual published DLL and binds to the
    // runtime-provided $PORT, rather than a bare/broken command or a
    // hardcoded port that would ignore Railway's injected port.
    [Fact]
    public void Entrypoint_references_the_published_dll_and_binds_to_runtime_port()
    {
        var content = ReadDockerfile();

        var startLine = content
            .Split('\n')
            .FirstOrDefault(line =>
                line.TrimStart().StartsWith("ENTRYPOINT") ||
                line.TrimStart().StartsWith("CMD"));

        Assert.False(string.IsNullOrEmpty(startLine), "no ENTRYPOINT or CMD found in Dockerfile");

        // Must reference the real published DLL, not a bare/broken command.
        Assert.Contains("ProjectTracker.Api.dll", startLine);

        // Must bind to Railway's runtime-injected $PORT, not a hardcoded port
        // that would ignore it.
        Assert.Contains("${PORT", startLine);
    }

    // 6.4 Given src/api/railway.toml,
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
        return File.ReadAllText(FindRepoFile("src/api/railway.toml"));
    }

    private static string ReadDockerfile()
    {
        return File.ReadAllText(FindRepoFile("src/api/Dockerfile"));
    }

    private static string FindRepoFile(string relativePath)
    {
        var directory = AppContext.BaseDirectory;

        while (directory is not null && !File.Exists(Path.Combine(directory, relativePath)))
        {
            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.True(directory is not null, $"could not locate repo root containing {relativePath}");
        return Path.Combine(directory!, relativePath);
    }
}
