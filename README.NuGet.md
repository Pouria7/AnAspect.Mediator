# AnAspect.Mediator

A high-performance mediator for .NET with a flexible pipeline and ValueTask-first design.

Status: Early alpha. APIs may change.

Install

```bash
dotnet add package AnAspect.Mediator
```

Quick start

```csharp
using AnAspect.Mediator;

public record CreateUser(string Name) : IRequest<UserDto>;
public record UserDto(Guid Id, string Name);

public sealed class CreateUserHandler : IRequestHandler<CreateUser, UserDto>
{
    public ValueTask<UserDto> HandleAsync(CreateUser request, CancellationToken ct)
        => ValueTask.FromResult(new UserDto(Guid.NewGuid(), request.Name));
}

services.AddMediator(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly));

var result = await mediator.SendAsync(new CreateUser("Alice"));
```

Pipeline control

```csharp
// Skip entire pipeline
await mediator.WithoutPipeline().SendAsync(cmd);

// Use only a specific group
await mediator.WithPipelineGroup("admin").SendAsync(cmd);

// Exclude by marker interface
await mediator.ExcludeBehavior<ILoggingBehavior>().SendAsync(cmd);

// Exclude a typed behavior implementing IPipelineBehavior<TRequest, TResponse>
await mediator.ExcludeBehavior<CreateUserValidation, CreateUser, UserDto>().SendAsync(cmd);

// Skip only global (object) behaviors; typed behaviors still run
await mediator.SkipGlobalBehaviors().SendAsync(cmd);
```

Type-safe open generic behaviors

```csharp
public interface IGlobalValidation<TReq, TRes> : IPipelineBehavior<TReq, TRes>
    where TReq : IRequest<TRes>;

public sealed class GlobalValidation<TReq, TRes> : IGlobalValidation<TReq, TRes>
    where TReq : IRequest<TRes>
{
    public async ValueTask<TRes> HandleAsync(TReq request, PipelineDelegate<TRes> next, CancellationToken ct)
        => await next();
}

cfg.AddBehavior<IGlobalValidation<AnyRequest, AnyResponse>, AnyRequest, AnyResponse>(order: 4);
```

Performance

- Low allocations, fast dispatch (see benchmarks in repo)
- Scales well with many handlers

Tests

- Added 13 new tests covering typed ExcludeBehavior overload
- All tests passing

License: MIT  •  Repo: https://github.com/Pouria7/AnAspect.Mediator
