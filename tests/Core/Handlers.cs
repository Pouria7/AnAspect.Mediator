
using AnAspect.Mediator.Abstractions;

namespace AnAspect.Mediator.Tests.Core;


public class CreateUserHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    public ValueTask<UserDto> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new UserDto(Guid.NewGuid(), request.Name, request.Email);
        return ValueTask.FromResult(user);
    }
}

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto?>
{
    public ValueTask<UserDto?> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
    {
        // Simulate lookup
        if (request.Id == Guid.Empty)
            return ValueTask.FromResult<UserDto?>(null);

        return ValueTask.FromResult<UserDto?>(new UserDto(request.Id, "Found User", "found@example.com"));
    }
}

public class DeleteUserHandler : IRequestHandler<DeleteUserCommand, bool>
{
    public ValueTask<bool> HandleAsync(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(request.Id != Guid.Empty);
    }
}

public class LogMessageHandler : IRequestHandler<LogMessageCommand, Unit>
{
    public static string? LastMessage { get; set; }

    public ValueTask<Unit> HandleAsync(LogMessageCommand request, CancellationToken cancellationToken)
    {
        LastMessage = request.Message;
        return Unit.ValueTask;
    }
}

// Abstract base handler pattern - simulates your CommandHandlerBase
public abstract class TransactionalHandlerBase<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken)
    {
        // Begin transaction (simulated)
        BeforeHandle();

        var result = await HandleCoreAsync(request, cancellationToken);

        // Commit transaction (simulated)
        AfterHandle();

        return result;
    }

    protected virtual void BeforeHandle() { }
    protected virtual void AfterHandle() { }
    protected abstract ValueTask<TResponse> HandleCoreAsync(TRequest request, CancellationToken cancellationToken);
}

public class UpdateUserHandler : TransactionalHandlerBase<UpdateUserCommand, UserDto>
{
    public static bool TransactionCommitted { get; set; }

    protected override void AfterHandle()
    {
        TransactionCommitted = true;
    }

    protected override ValueTask<UserDto> HandleCoreAsync(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(new UserDto(request.Id, request.Name, "updated@example.com"));
    }
}

