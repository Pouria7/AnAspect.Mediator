namespace AnAspect.Mediator.Notification.Internals;

internal static class NotificationRunner
{
    public static Task<NotificationPushResult> ExecuteAsync(
        IServiceProvider serviceProvider,
        INotification notification,
        Type notificationType,
        Type[] handlerTypes,
        NotificationPushConfig config,
        CancellationToken cancellationToken)
    {
        var activeHandlers = FilterHandlers(handlerTypes, config.ExcludedHandlers);
        if (activeHandlers.Length == 0)
            return NotificationPushResult.EmptyTask;

        return config.Strategy == NotificationPublishStrategy.Concurrent
            ? ExecuteConcurrentAsync(serviceProvider, notification, notificationType, activeHandlers, config, cancellationToken)
            : ExecuteSequentialAsync(serviceProvider, notification, notificationType, activeHandlers, config, cancellationToken);
    }

    private static async Task<NotificationPushResult> ExecuteSequentialAsync(
        IServiceProvider serviceProvider,
        INotification notification,
        Type notificationType,
        Type[] activeHandlers,
        NotificationPushConfig config,
        CancellationToken cancellationToken)
    {
        var total = activeHandlers.Length;

        if (cancellationToken.IsCancellationRequested)
        {
            return new NotificationPushResult(
                total: total,
                succeeded: 0,
                failed: 0,
                unprocessed: total,
                failures: [],
                unprocessedHandlers: activeHandlers);
        }

        var succeeded = 0;
        var failed = 0;
        List<NotificationFailure>? failures = null;
        List<Type>? unprocessedHandlers = null;

        for (var i = 0; i < total; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                unprocessedHandlers ??= new List<Type>(total - i);
                for (var r = i; r < total; r++)
                    unprocessedHandlers.Add(activeHandlers[r]);
                break;
            }

            var handlerType = activeHandlers[i];
            Task<NotificationHandlerResult> task;

            try
            {
                var handler = (INotificationHandler)serviceProvider.GetRequiredService(handlerType);
                task = handler.HandleAsync(notification, cancellationToken);
            }
            catch (Exception syncEx)
            {
                task = Task.FromException<NotificationHandlerResult>(syncEx);
            }

            if (!task.IsCompleted)
            {
                await ((Task)task).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }

            if (task.IsCanceled || (task.IsFaulted && IsCancellationException(task.Exception, cancellationToken)))
            {
                unprocessedHandlers ??= new List<Type>(total - i);
                for (var r = i; r < total; r++)
                    unprocessedHandlers.Add(activeHandlers[r]);
                break;
            }

            if (task.IsFaulted)
            {
                var ex = UnwrapException(task.Exception!);
                failed++;
                failures ??= [];
                failures.Add(new NotificationFailure(handlerType, notificationType, ex.Message, ex));

                if (config.StopOnFirstFailure && i + 1 < total)
                {
                    unprocessedHandlers ??= new List<Type>(total - (i + 1));
                    for (var r = i + 1; r < total; r++)
                        unprocessedHandlers.Add(activeHandlers[r]);
                    break;
                }

                continue;
            }

            var handlerResult = task.Result;
            if (handlerResult.IsSuccess)
            {
                succeeded++;
            }
            else
            {
                failed++;
                failures ??= [];
                failures.Add(new NotificationFailure(
                    handlerType,
                    notificationType,
                    handlerResult.ErrorMessage,
                    handlerResult.Exception));

                if (config.StopOnFirstFailure && i + 1 < total)
                {
                    unprocessedHandlers ??= new List<Type>(total - (i + 1));
                    for (var r = i + 1; r < total; r++)
                        unprocessedHandlers.Add(activeHandlers[r]);
                    break;
                }
            }
        }

        var unprocessedCount = unprocessedHandlers?.Count ?? 0;
        var result = new NotificationPushResult(
            total: total,
            succeeded: succeeded,
            failed: failed,
            unprocessed: unprocessedCount,
            failures: failures,
            unprocessedHandlers: unprocessedHandlers);

        if (!config.SuppressExceptions && result.HasFailures)
        {
            result.EnsureSuccess();
        }

        return result;
    }

    private static async Task<NotificationPushResult> ExecuteConcurrentAsync(
        IServiceProvider serviceProvider,
        INotification notification,
        Type notificationType,
        Type[] activeHandlers,
        NotificationPushConfig config,
        CancellationToken cancellationToken)
    {
        var total = activeHandlers.Length;

        if (cancellationToken.IsCancellationRequested)
        {
            return new NotificationPushResult(
                total: total,
                succeeded: 0,
                failed: 0,
                unprocessed: total,
                failures: [],
                unprocessedHandlers: activeHandlers);
        }

        var tasks = new Task<NotificationHandlerResult>[total];

        for (var i = 0; i < total; i++)
        {
            var handlerType = activeHandlers[i];
            try
            {
                var handler = (INotificationHandler)serviceProvider.GetRequiredService(handlerType);
                tasks[i] = handler.HandleAsync(notification, cancellationToken);
            }
            catch (Exception syncEx)
            {
                tasks[i] = Task.FromException<NotificationHandlerResult>(syncEx);
            }
        }

        await ((Task)Task.WhenAll(tasks)).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        var succeeded = 0;
        var failed = 0;
        List<NotificationFailure>? failures = null;
        List<Type>? unprocessedHandlers = null;

        for (var i = 0; i < total; i++)
        {
            var handlerType = activeHandlers[i];
            var task = tasks[i];

            if (task.IsCanceled || (task.IsFaulted && IsCancellationException(task.Exception, cancellationToken)))
            {
                unprocessedHandlers ??= [];
                unprocessedHandlers.Add(handlerType);
                continue;
            }

            if (task.IsFaulted)
            {
                var ex = UnwrapException(task.Exception!);
                failed++;
                failures ??= [];
                failures.Add(new NotificationFailure(handlerType, notificationType, ex.Message, ex));
                continue;
            }

            var handlerResult = task.Result;
            if (handlerResult.IsSuccess)
            {
                succeeded++;
            }
            else
            {
                failed++;
                failures ??= [];
                failures.Add(new NotificationFailure(
                    handlerType,
                    notificationType,
                    handlerResult.ErrorMessage,
                    handlerResult.Exception));
            }
        }

        var unprocessedCount = unprocessedHandlers?.Count ?? 0;
        var result = new NotificationPushResult(
            total: total,
            succeeded: succeeded,
            failed: failed,
            unprocessed: unprocessedCount,
            failures: failures,
            unprocessedHandlers: unprocessedHandlers);

        if (!config.SuppressExceptions && result.HasFailures)
        {
            result.EnsureSuccess();
        }

        return result;
    }

    private static Type[] FilterHandlers(Type[] handlerTypes, IReadOnlySet<Type> excludedHandlers)
    {
        if (excludedHandlers.Count == 0)
            return handlerTypes;

        return handlerTypes
            .Where(t => !excludedHandlers.Contains(t))
            .ToArray();
    }

    private static bool IsCancellationException(AggregateException? exception, CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested || exception is null)
            return false;

        var inner = UnwrapException(exception);
        return inner is OperationCanceledException;
    }

    private static Exception UnwrapException(AggregateException aggregateException) =>
        aggregateException.InnerExceptions.Count == 1
            ? aggregateException.InnerExceptions[0]
            : aggregateException.Flatten();
}
