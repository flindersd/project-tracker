using System;
using System.Collections.Generic;
using Npgsql;

namespace ProjectTracker.Api.Shared;

/// <summary>
/// Translates a libpq-style DATABASE_URL (postgresql://...) into a PostgreSQL connection string
/// suitable for Npgsql. In non-Development environments, the result must not be null.
/// </summary>
public static class DatabaseConnectionString
{
    public static string Resolve(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["ConnectionStrings:Postgres"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return IsLibpqUri(configured) ? Translate(configured) : configured;
        }

        var databaseUrl = configuration["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return IsLibpqUri(databaseUrl) ? Translate(databaseUrl) : databaseUrl;
        }

        if (environment.IsDevelopment())
        {
            return Normalize("Host=localhost;Port=5432;Database=projecttracker;Username=postgres;Password=postgres");
        }

        throw new ConfigurationException("A database connection string is required. Set ConnectionStrings:Postgres or DATABASE_URL.");
    }

    private static bool IsLibpqUri(string value) =>
        value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase);

    private static bool IsLocalHost(string host) =>
        string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            SslMode = SslMode.Require,
        };

        return builder.ConnectionString;
    }

    public static string Translate(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new FormatException("Connection string must not be null or empty.");
        }

        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed) ||
            !string.Equals(parsed.Scheme, "postgresql", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(parsed.Scheme, "postgres", StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException($"Invalid connection string format. Expected a postgresql:// URI, got '{uri}'.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = parsed.Host,
            Port = parsed.IsDefaultPort ? 5432 : parsed.Port,
            Database = parsed.AbsolutePath.Trim('/'),
            SslMode = IsLocalHost(parsed.Host) ? SslMode.Disable : SslMode.Require,
        };

        var userInfo = parsed.UserInfo;
        if (!string.IsNullOrEmpty(userInfo))
        {
            var parts = userInfo.Split(':', 2, StringSplitOptions.None);
            builder.Username = Uri.UnescapeDataString(parts[0]);
            if (parts.Length > 1)
            {
                builder.Password = Uri.UnescapeDataString(parts[1]);
            }
        }

        foreach (var parameter in ParseQuery(parsed.Query))
        {
            builder[parameter.Key] = parameter.Value;
        }

        return builder.ConnectionString;
    }

    private static IEnumerable<KeyValuePair<string, string>> ParseQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            yield break;
        }

        var trimmed = query.TrimStart('?');
        if (trimmed.Length == 0)
        {
            yield break;
        }

        foreach (var pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            yield return new KeyValuePair<string, string>(key, value);
        }
    }
}
