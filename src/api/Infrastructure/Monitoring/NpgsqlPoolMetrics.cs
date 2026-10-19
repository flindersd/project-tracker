using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Hosting;
using Npgsql;
using ProjectTracker.Api.Shared;

namespace ProjectTracker.Api.Infrastructure.Monitoring;

public sealed class NpgsqlPoolMetricsTracker
{
    private long _activeConnections;

    public long ActiveConnections => Interlocked.Read(ref _activeConnections);

    public void ConnectionOpened() => Interlocked.Increment(ref _activeConnections);

    public void ConnectionClosed()
    {
        var next = Interlocked.Decrement(ref _activeConnections);
        if (next < 0)
        {
            Interlocked.Exchange(ref _activeConnections, 0);
        }
    }
}

public sealed class NpgsqlPoolMetricsInterceptor : DbConnectionInterceptor
{
    private readonly NpgsqlPoolMetricsTracker _tracker;

    public NpgsqlPoolMetricsInterceptor(NpgsqlPoolMetricsTracker tracker)
    {
        _tracker = tracker;
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        if (connection is NpgsqlConnection)
        {
            _tracker.ConnectionOpened();
        }

        base.ConnectionOpened(connection, eventData);
    }

    public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        if (connection is NpgsqlConnection)
        {
            _tracker.ConnectionOpened();
        }

        return base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    public override void ConnectionClosed(DbConnection connection, ConnectionEndEventData eventData)
    {
        if (connection is NpgsqlConnection)
        {
            _tracker.ConnectionClosed();
        }

        base.ConnectionClosed(connection, eventData);
    }

    public override void ConnectionFailed(DbConnection connection, ConnectionErrorEventData eventData)
    {
        if (connection is NpgsqlConnection)
        {
            _tracker.ConnectionClosed();
        }

        base.ConnectionFailed(connection, eventData);
    }
}

public sealed class NpgsqlPoolMetricsBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<NpgsqlPoolMetricsBackgroundService> _logger;
    private readonly NpgsqlPoolMetricsTracker _tracker;

    public NpgsqlPoolMetricsBackgroundService(
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<NpgsqlPoolMetricsBackgroundService> logger,
        NpgsqlPoolMetricsTracker tracker)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
        _tracker = tracker;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        LogPoolState();

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            LogPoolState();
        }
    }

    private void LogPoolState()
    {
        var connectionString = DatabaseConnectionString.Resolve(_configuration, _environment);
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var maxPoolSize = builder.MaxPoolSize;
        var activeConnections = _tracker.ActiveConnections;
        var utilization = maxPoolSize <= 0 ? 0d : (double)activeConnections / maxPoolSize;

        if (activeConnections >= maxPoolSize)
        {
            _logger.LogWarning(
                "Npgsql pool saturation warning: active_connections={ActiveConnections} max_pool_size={MaxPoolSize} pool_utilization={PoolUtilization:P0} db_host={DbHost} db_name={DbName}",
                activeConnections,
                maxPoolSize,
                utilization,
                builder.Host,
                builder.Database);
            return;
        }

        _logger.LogInformation(
            "Npgsql pool metrics: active_connections={ActiveConnections} max_pool_size={MaxPoolSize} pool_utilization={PoolUtilization:P0} db_host={DbHost} db_name={DbName}",
            activeConnections,
            maxPoolSize,
            utilization,
            builder.Host,
            builder.Database);
    }
}
