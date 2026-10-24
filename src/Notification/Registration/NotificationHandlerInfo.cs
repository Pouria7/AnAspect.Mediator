namespace AnAspect.Mediator.Notification.Registration;

/// <summary>
/// Extracted metadata about a concrete <see cref="INotificationHandler{TNotification}"/> implementation.
/// </summary>
internal readonly record struct NotificationHandlerInfo(
    Type HandlerType,
    Type HandlerInterface,
    Type NotificationType);
