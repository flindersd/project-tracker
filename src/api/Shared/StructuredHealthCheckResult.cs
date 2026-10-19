using System;
using System.Collections.Generic;

namespace ProjectTracker.Api.Shared;

/// <summary>
/// Represents a structured health check result with per-check status breakdown.
/// Used as the response body for the /healthz/ready endpoint.
/// </summary>
public sealed class StructuredHealthCheckResult
{
    /// <summary>
    /// List of individual check results.
    /// </summary>
    public List<HealthCheckEntry> Checks { get; } = new List<HealthCheckEntry>();

    /// <summary>
    /// Overall status of the health check.
    /// </summary>
    public string Status { get; set; } = "unknown";

    public void AddCheck(string name, bool isHealthy)
    {
        Checks.Add(new HealthCheckEntry(name, isHealthy));
    }

    public void AddCheck(string name, string message, Exception? innerException)
    {
        Checks.Add(new HealthCheckEntry(name, false, message, innerException));
    }

    public void SetStatus(string status)
    {
        Status = status;
    }
}

/// <summary>
/// Represents a single health check entry.
/// </summary>
public sealed record HealthCheckEntry(
    string Name,
    bool IsHealthy,
    string? Message = null,
    Exception? InnerException = null
);
