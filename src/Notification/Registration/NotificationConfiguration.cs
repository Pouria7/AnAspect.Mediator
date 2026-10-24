using AnAspect.Mediator.Registration;

namespace AnAspect.Mediator.Notification.Registration;

/// <summary>
/// Configuration options for <c>AddNotification</c>.
/// </summary>
public sealed class NotificationConfiguration
{
    internal List<Assembly> Assemblies { get; } = new();

    /// <summary>
    /// Service lifetime for registered <see cref="INotificationHandler{TNotification}"/> implementations.
    /// Defaults to <see cref="ServiceLifetime.Transient"/>.
    /// </summary>
    public ServiceLifetime HandlerLifetime { get; set; } = ServiceLifetime.Transient;

    /// <summary>
    /// Controls how notification models with no registered handler are handled during registration.
    /// Defaults to <see cref="RegistrationDiagnosticPolicy.Warning"/>.
    /// If set to <see cref="RegistrationDiagnosticPolicy.None"/>, notification model scanning is bypassed.
    /// </summary>
    public RegistrationDiagnosticPolicy MissingHandlerPolicy { get; set; } = RegistrationDiagnosticPolicy.Warning;

    /// <summary>
    /// Default execution strategy when <c>PushAsync</c> is called without explicit strategy overrides.
    /// Defaults to <see cref="NotificationPublishStrategy.Sequential"/>.
    /// </summary>
    public NotificationPublishStrategy DefaultStrategy { get; set; } = NotificationPublishStrategy.Sequential;

    /// <summary>
    /// Default setting for stopping on the first handler failure in sequential mode.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool DefaultStopOnFirstFailure { get; set; }

    /// <summary>
    /// Default setting for suppressing exceptions and returning them inside <see cref="NotificationPushResult"/> (Result Pattern).
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool DefaultSuppressExceptions { get; set; } = true;

    public NotificationConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        Assemblies.Add(assembly);
        return this;
    }

    public NotificationConfiguration RegisterServicesFromAssemblies(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        Assemblies.AddRange(assemblies);
        return this;
    }

    public NotificationConfiguration RegisterServicesFromAssemblyContaining<T>() =>
        RegisterServicesFromAssembly(typeof(T).Assembly);
}
