using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace ProjectTracker.Api.Tests.Platform;

/// <summary>
/// Given/When/Then source: Platform/gherkin-scenarios.md, section 5.
/// Verifies D-5: EF Core's command-level logger must never run at Information,
/// because that level includes bound parameter values — i.e. user board content —
/// in the log stream.
/// </summary>
public class LoggingConfigurationTests
{
    private static readonly string[] DisallowedVerboseLevels = { "Information", "Debug", "Trace" };

    public static TheoryData<string> AppSettingsFiles => new()
    {
        FindRepoFile("src/api/appsettings.json"),
        FindRepoFile("src/api/appsettings.Development.json"),
    };

    // 5.1 Given the application's logging configuration,
    // When the Microsoft.EntityFrameworkCore.Database.Command category is inspected,
    // Then its configured level is Warning or higher in every environment.
    [Theory]
    [MemberData(nameof(AppSettingsFiles))]
    public void EfCoreCommandLogging_is_never_configured_more_verbose_than_warning(string appSettingsPath)
    {
        Assert.True(File.Exists(appSettingsPath), $"expected {appSettingsPath} to exist");

        using var document = JsonDocument.Parse(File.ReadAllText(appSettingsPath));

        var hasLogging = document.RootElement.TryGetProperty("Logging", out var logging);
        Assert.True(hasLogging, $"{appSettingsPath} has no Logging section");

        var hasLogLevel = logging.TryGetProperty("LogLevel", out var logLevel);
        Assert.True(hasLogLevel, $"{appSettingsPath} has no Logging:LogLevel section");

        var hasEfCommandCategory = logLevel.TryGetProperty(
            "Microsoft.EntityFrameworkCore.Database.Command", out var efCommandLevel);

        Assert.True(
            hasEfCommandCategory,
            $"{appSettingsPath} must explicitly pin Microsoft.EntityFrameworkCore.Database.Command (it currently inherits a more verbose default)");

        var configuredLevel = efCommandLevel.GetString();

        Assert.False(
            DisallowedVerboseLevels.Contains(configuredLevel),
            $"{appSettingsPath} configures Microsoft.EntityFrameworkCore.Database.Command at '{configuredLevel}', which logs parameter values");
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
