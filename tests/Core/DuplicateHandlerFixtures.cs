using AnAspect.Mediator.Abstractions;

namespace AnAspect.Mediator.Tests.Core;

// Dedicated request type used only by duplicate-handler tests, so it doesn't
// interfere with assertions in other tests that scan the same assembly.
public record PingCommand(string Message) : IRequest<string>;

public class PingHandlerOne : IRequestHandler<PingCommand, string>
{
    public ValueTask<string> HandleAsync(PingCommand request, CancellationToken cancellationToken)
        => ValueTask.FromResult($"One:{request.Message}");
}

public class PingHandlerTwo : IRequestHandler<PingCommand, string>
{
    public ValueTask<string> HandleAsync(PingCommand request, CancellationToken cancellationToken)
        => ValueTask.FromResult($"Two:{request.Message}");
}
