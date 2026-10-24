# AnAspect.Mediator.Notification

A lightweight, high-performance notification and domain-event publisher for .NET, built to complement [`AnAspect.Mediator`](../../README.md) for DDD and event-driven architectures without external boilerplate.

> 🚀 **Status**: Stable Release (`v1.0.0`).

## Features

* **Custom Awaitable (`NotificationPushAwaitable`)**: Deferred execution until `await`, enabling fluent per-call configuration (`.AsSequential()`, `.AsConcurrent()`, `.StopOnFirstFailure()`, `.ExcludeHandler<T>()`, `.ThrowOnFailure()`).
* **Complete Execution Breakdown (`NotificationPushResult`)**: Every push returns accurate counts for `Total`, `Succeeded`, `Failed`, and `Unprocessed` handlers, along with detailed `Failures` and `UnprocessedHandlers`.
* **Result Pattern Support (`NotificationHandlerResult`)**: Handlers can return failures (`NotificationHandlerResult.Fail("reason")` or `return false;`) without the CPU/memory overhead of throwing exceptions.
* **Zero Async Exception Rethrow Overhead**: Uses `.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing)` internally so faulted handler tasks are inspected and recorded without expensive async stack unwinding.
* **Sequential & Concurrent Dispatch**:
  * **Sequential** *(Default)*: Executes handlers one by one (safe for shared scoped services like EF Core `DbContext`).
  * **Concurrent**: Executes all handlers in parallel via `Task.WhenAll`.
* **Fail-Fast Registration Diagnostics**: Shares `RegistrationDiagnosticPolicy` (`None` | `Warning` | `Throw`) with `AnAspect.Mediator.Core` for missing notification handler detection at startup.

---

## Quick Start

### 1. Installation

```bash
dotnet add package AnAspect.Mediator.Notification
```

### 2. Define a Notification and Handlers

```csharp
using AnAspect.Mediator.Notification;

// Notification / Domain Event (supports custom derived interfaces like IDomainEvent : INotification)
public record OrderPlacedNotification(Guid OrderId, decimal Amount) : INotification;

// Handler 1: Synchronous completion via cached SuccessTask
public sealed class UpdateInventoryHandler : INotificationHandler<OrderPlacedNotification>
{
    public Task<NotificationHandlerResult> HandleAsync(
        OrderPlacedNotification notification,
        CancellationToken cancellationToken)
    {
        return NotificationHandlerResult.SuccessTask;
    }
}

// Handler 2: Async handler using Result Pattern (no exception overhead on failure)
public sealed class SendOrderEmailHandler : INotificationHandler<OrderPlacedNotification>
{
    public async Task<NotificationHandlerResult> HandleAsync(
        OrderPlacedNotification notification,
        CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);

        if (notification.Amount <= 0)
            return NotificationHandlerResult.Fail("Invalid order amount for email receipt.");

        return true; // Implicit conversion to NotificationHandlerResult.Success
    }
}
```

### 3. Register Services

```csharp
services.AddNotification(typeof(UpdateInventoryHandler).Assembly);
```

Or with full configuration:

```csharp
services.AddNotification(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(UpdateInventoryHandler).Assembly);
    cfg.HandlerLifetime = ServiceLifetime.Transient; // Default: Transient
    cfg.DefaultStrategy = NotificationPublishStrategy.Sequential; // Sequential (default) | Concurrent
    cfg.DefaultStopOnFirstFailure = false;
    cfg.DefaultSuppressExceptions = true;
    cfg.MissingHandlerPolicy = RegistrationDiagnosticPolicy.Warning; // None | Warning (default) | Throw
});
```

---

## 🚀 Usage & Fluent Control

### Default Sequential Push & Breakdown Inspection

```csharp
public class OrderService(INotificationPublisher publisher)
{
    public async Task CompleteOrderAsync(Guid orderId, decimal amount, CancellationToken ct)
    {
        NotificationPushResult result = await publisher.PushAsync(
            new OrderPlacedNotification(orderId, amount), ct);

        // Always satisfies: Total == Succeeded + Failed + Unprocessed
        Console.WriteLine($"Total: {result.Total}, Succeeded: {result.Succeeded}, Failed: {result.Failed}, Unprocessed: {result.Unprocessed}");
        Console.WriteLine($"Status: {result.Status}"); // NoHandlers | AllSucceeded | PartialSuccess | AllFailed | Cancelled

        if (result.HasFailures)
        {
            foreach (var failure in result.Failures)
            {
                Console.WriteLine($"Handler {failure.HandlerType.Name} failed: {failure.ErrorMessage}");
            }
        }
    }
}
```

### Concurrent vs. Sequential Execution

```csharp
// Run all handlers concurrently in parallel
NotificationPushResult concurrentResult = await publisher
    .PushAsync(notification, ct)
    .AsConcurrent();

// Or via pre-push fluent builder / shorthand extension
NotificationPushResult result2 = await publisher.AsConcurrent().PushAsync(notification, ct);
NotificationPushResult result3 = await publisher.PushConcurrentAsync(notification, ct);
```

### Stop on First Failure (`Unprocessed` Handlers)

In sequential mode, `StopOnFirstFailure()` halts execution as soon as a handler fails (via `NotificationHandlerResult.Fail` or an unhandled exception) and records all remaining handlers in `UnprocessedHandlers`:

```csharp
NotificationPushResult result = await publisher
    .PushAsync(notification, ct)
    .AsSequential()
    .StopOnFirstFailure();

// If Handler 1 succeeded, Handler 2 failed, and Handler 3 was skipped:
// result.Total == 3, result.Succeeded == 1, result.Failed == 1, result.Unprocessed == 1
// result.UnprocessedHandlers contains [typeof(Handler3)]
```

### Excluding Specific Handlers at Runtime

```csharp
NotificationPushResult result = await publisher
    .PushAsync(notification, ct)
    .ExcludeHandler<SendOrderEmailHandler>();
```

### Throwing Exceptions on Failure

By default, `SuppressExceptions` is `true` so failures are returned cleanly inside `NotificationPushResult`. If you want failed handlers to throw an `AggregateException`:

```csharp
await publisher
    .PushAsync(notification, ct)
    .ThrowOnFailure();

// Or call EnsureSuccess() manually on the result:
result.EnsureSuccess();
```

---

## License

This project is licensed under the MIT License. See the [LICENSE](../../LICENSE.txt) file for more details.
