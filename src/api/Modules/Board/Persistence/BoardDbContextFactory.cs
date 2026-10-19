using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ProjectTracker.Api.Modules.Board.Access;
using ProjectTracker.Api.Shared;

namespace ProjectTracker.Api.Modules.Board.Persistence;

/// <summary>
/// Design-time factory for <see cref="BoardDbContext"/> so EF Core can build
/// migration bundles without needing the full application host.
/// </summary>
public sealed class BoardDbContextFactory : IDesignTimeDbContextFactory<BoardDbContext>
{
    public BoardDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var environmentName = configuration["DOTNET_ENVIRONMENT"]
            ?? configuration["ASPNETCORE_ENVIRONMENT"]
            ?? Environments.Development;

        var environment = new DesignTimeHostEnvironment(environmentName);
        var connectionString = DatabaseConnectionString.Resolve(configuration, environment);

        var optionsBuilder = new DbContextOptionsBuilder<BoardDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory_Board");
        });

        return new BoardDbContext(optionsBuilder.Options, new BoardScopeAccessor());
    }

    private sealed class DesignTimeHostEnvironment : IHostEnvironment
    {
        public DesignTimeHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string ApplicationName { get; set; } = typeof(BoardDbContextFactory).Assembly.GetName().Name ?? "ProjectTracker.Api";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public string EnvironmentName { get; set; }
    }
}
