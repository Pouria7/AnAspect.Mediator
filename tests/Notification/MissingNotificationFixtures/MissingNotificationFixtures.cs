using AnAspect.Mediator.Notification;

namespace AnAspect.Mediator.Notification.Tests.MissingFixtures;

public interface ICustomDomainEvent : INotification;

public record OrphanOrderCancelledNotification(Guid OrderId) : INotification;

public record OrphanDomainEvent(string Reason) : ICustomDomainEvent;

public abstract record AbstractBaseNotification(Guid Id) : INotification;

public record OpenGenericNotification<T>(T Data) : INotification;

public record HandledFixtureNotification(string Value) : INotification;

public sealed class HandledFixtureNotificationHandler : INotificationHandler<HandledFixtureNotification>
{
    public Task<NotificationHandlerResult> HandleAsync(HandledFixtureNotification notification, CancellationToken cancellationToken) =>
        NotificationHandlerResult.SuccessTask;
}
