using AnAspect.Mediator.Abstractions;

namespace AnAspect.Mediator.Tests.DuplicateFixtures;

// This type set lives in its own assembly, isolated from the main test
// assembly, so that scanning it for duplicate handlers doesn't affect
// any other test that scans AnAspect.Mediator.Tests (e.g. via
// typeof(CreateUserHandler).Assembly).
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
