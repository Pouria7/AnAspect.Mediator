using AnAspect.Mediator;

namespace AnAspect.Mediator.Tests.MissingHandlerFixtures;

// Isolated fixture assembly containing request models without handlers
public record OrphanCommand(string Message) : IRequest<string>;

public record OrphanVoidCommand(string Message) : IRequest;

public interface ICustomQuery<out T> : IRequest<T>;

public record OrphanCustomQuery(int Id) : ICustomQuery<int>;

public abstract record AbstractBaseCommand(string Message) : IRequest<string>;

public record OpenGenericCommand<T>(T Value) : IRequest<T>;

// Request that DOES have a handler in this assembly
public record HandledInMissingFixtureCommand(string Text) : IRequest<string>;

public class HandledInMissingFixtureCommandHandler : IRequestHandler<HandledInMissingFixtureCommand, string>
{
    public ValueTask<string> HandleAsync(HandledInMissingFixtureCommand request, CancellationToken cancellationToken)
        => ValueTask.FromResult($"Handled:{request.Text}");
}
