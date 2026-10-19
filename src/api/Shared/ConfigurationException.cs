using System;

namespace ProjectTracker.Api;

/// <summary>
/// Represents a configuration error that prevents the application from starting.
/// This is used to fail fast when required configuration values are missing in
/// non-development environments.
/// </summary>
public sealed class ConfigurationException : Exception
{
    public ConfigurationException(string message) : base(message)
    {
    }

    public ConfigurationException(string message, Exception? innerException = null) : base(message, innerException)
    {
    }
}
