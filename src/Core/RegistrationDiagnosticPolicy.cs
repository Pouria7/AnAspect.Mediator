namespace AnAspect.Mediator.Registration;

/// <summary>
/// Controls how the mediator or notification publisher reacts to a registration-time diagnostic
/// (e.g. duplicate handlers, requests or notifications with no handler).
/// </summary>
public enum RegistrationDiagnosticPolicy
{
    /// <summary>
    /// Ignore the condition silently.
    /// </summary>
    None,

    /// <summary>
    /// Log a warning via <see cref="Microsoft.Extensions.Logging.ILogger"/> but continue. Default.
    /// </summary>
    Warning,

    /// <summary>
    /// Throw during registration (or, for conditions that can only be detected at
    /// dispatch time, at the moment the condition is hit).
    /// </summary>
    Throw
}
