# AnAspect.Mediator

A high-performance mediator implementation for .NET using `ValueTask` for optimized performance and minimal memory allocation.

> ⚠️ **Status**: Early development. Test coverage is ongoing. Not recommended for production use.

## Features

* **High Performance**: Uses `ValueTask<T>` for reduced memory allocations, outperforming popular alternatives
* **Advanced Pipeline System**: Flexible behavior pipeline with grouping, exclusion, ordering, and type-safe open generics
* **Simple API**: Clean, intuitive interface for request/response pattern
* **Native DI Integration**: Seamless integration with Microsoft's DI container
* **Flexible Handler Registration**: Automatic assembly scanning with minimal configuration
* **Type-Safe Open Generics**: Elegant generic behavior support with compile-time safety
* **Custom Request Interfaces**: Support for ICommand, IQuery, and custom patterns
* **Unit Support**: Built-in `Unit` type for requests without responses

## Quick Start

### 1. Installation

```bash
dotnet add package AnAspect.Mediator
```

### 2. Define your requests and handlers

```csharp
using AnAspect.Mediator;

// Request with response
public record CreateUserCommand(string Name, string Email) : IRequest<UserDto>;

public record UserDto(Guid Id, string Name, string Email);

// Handler
public class CreateUserHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    public ValueTask<UserDto> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new UserDto(Guid.NewGuid(), request.Name, request.Email);
        return ValueTask.FromResult(user);
    }
}
```

### 3. Register services

```csharp
services.AddMediator(typeof(CreateUserHandler).Assembly);
```

### 4. Use the mediator

```csharp
public class UserController
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<UserDto> CreateUser(string name, string email)
    {
        var command = new CreateUserCommand(name, email);
        return await _mediator.SendAsync(command);
    }
}
```

## 🚀 Advanced Pipeline System

AnAspect.Mediator provides a sophisticated pipeline system with powerful features like behavior grouping, exclusion, ordering, and elegant type-safe open generics.

### Elegant Type-Safe Open Generics

```csharp
// Define a generic behavior interface
public interface IGlobalValidationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse> 
    where TRequest : IRequest<TResponse>;

// Implement for all requests
public class GlobalGenericValidation<TRequest, TResponse> 
    : IGlobalValidationBehavior<TRequest, TResponse> 
    where TRequest : IRequest<TResponse>
{
    public async ValueTask<TResponse> HandleAsync(
        TRequest request, 
        PipelineDelegate<TResponse> next, 
        CancellationToken ct)
    {
        // Global validation logic for ALL requests
        Console.WriteLine($"Validating {typeof(TRequest).Name}");
        return await next();
    }
}

// Register with type safety
cfg.AddBehavior<IGlobalValidationBehavior<AnyRequest,AnyResponse>,AnyRequest,AnyResponse>(order: 4);
```

### Complete Pipeline Configuration

```csharp
services.AddMediator(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
    
    // Global behaviors (applied to all requests)
    cfg.AddBehavior<LoggingBehavior>(order: 10);
    cfg.AddBehavior<PerformanceBehavior>(order: 20, lifetime: ServiceLifetime.Transient);
    
    // Typed behaviors (applied to specific request types)
    cfg.AddBehavior<CreateUserValidation, CreateUserCommand, UserDto>(order: 15);
    cfg.AddBehavior<GetUserCaching, GetUserQuery, UserDto?>(order: 5);
    
    // Grouped behaviors (applied only when group is active)
    cfg.AddBehavior<TransactionBehavior>(order: 1, groups: ["admin"]);
});
```

### Runtime Pipeline Control

```csharp
// Skip all pipeline behaviors
await _mediator.WithoutPipeline().SendAsync(command);

// Use specific pipeline group
await _mediator.WithPipelineGroup("admin").SendAsync(command);

// Exclude specific behavior types
await _mediator
    .ExcludeBehavior<ILoggingBehavior>()
    .ExcludeBehavior<CreateUserValidation, CreateUserCommand, UserDto>()
    .ExcludeBehavior<IGlobalValidationBehavior<AnyRequest,AnyResponse>, AnyRequest, AnyResponse>()
    .SendAsync(command);

// Skip only global behaviors
await _mediator.SkipGlobalBehaviors().SendAsync(command);
```

### Custom Request Interfaces

```csharp
public interface ICommand<out TResponse> : IRequest<TResponse> { }
public interface IQuery<out TResponse> : IRequest<TResponse> { }

public record GetUserQuery(Guid Id) : IQuery<UserDto?>;
```

### Requests Without Response

```csharp
public record LogMessageCommand(string Message) : IRequest;

public class LogMessageHandler : IRequestHandler<LogMessageCommand>
{
    public ValueTask<Unit> HandleAsync(LogMessageCommand request, CancellationToken cancellationToken)
    {
        Console.WriteLine(request.Message);
        return Unit.ValueTask;
    }
}
```

## 📊 Performance Benchmarks

AnAspect.Mediator is engineered for maximum throughput and minimal memory allocation. Benchmarked against **MediatR v14.2.0** and **Mediator.SourceGenerator v3.0.2** on **.NET 10**.

### 🏆 Core Execution Performance

| Method | Return Type | Mean | Allocated | Performance Advantage |
| :--- | :--- | :--- | :--- | :--- |
| `MediatR (No Pipeline)` *(Baseline)* | `Task<T>` | 84.63 ns | 240 B | Baseline |
| **`AnAspect (No Pipeline)`** | `ValueTask<T>` | **54.21 ns** | **64 B** | 🚀 **36% faster**, **73% less memory** |
| **`AnAspect (No Pipeline, .AsTask())`** | `Task<T>` | **61.44 ns** | **136 B** | 🚀 **27% faster**, **43% less memory** |
| `MediatR (With 2 Behaviors)` | `Task<T>` | 195.67 ns | 768 B | Baseline |
| **`AnAspect (With 2 Behaviors)`** | `ValueTask<T>` | **177.65 ns** | **408 B** | 🚀 **9% faster**, **47% less memory** |
| **`AnAspect (With 2 Behaviors, .AsTask())`** | `Task<T>` | **175.48 ns** | **480 B** | 🚀 **10% faster**, **37% less memory** |

### 🥇 Scalability at Scale (50 & 100 Handlers in DI)

| Method | 50 Handlers | 100 Handlers | Memory Allocated | Scalability Profile |
| :--- | :--- | :--- | :--- | :--- |
| **`AnAspect.Mediator`** | **69.96 ns** | **67.70 ns** | **96 B** | 🥇 **Zero degradation ($O(1)$ flat)** |
| `SourceGenerator` | 81.03 ns | 77.04 ns | 160 B | 14% slower than AnAspect at 100 handlers |
| `MediatR` | 84.43 ns | 88.67 ns | 344 B | 3.5x higher memory allocation |

### ⚡ Cold Start Performance

| Method | Mean | Allocated | Advantage |
| :--- | :--- | :--- | :--- |
| **`AnAspect (No Pipeline)`** | **41,573 ns** | **64 B** | ⚡ **Lowest allocations (33,556 ns via .AsTask())** |
| `MediatR (No Pipeline)` | 42,777 ns | 304 B | Baseline |
| `SourceGenerator (No Pipeline)` | 65,498 ns | 40 B | Higher JIT initialization |

> 📖 **Full Benchmark Reports**: For in-depth analysis, pipeline depth scaling (1-5 behaviors), concurrent dispatch, and architectural deep dive, see [Detailed Benchmark Reports](./benchmarks/README.md).

## Configuration

```csharp
services.AddMediator(config => 
{
    config.RegisterServicesFromAssembly(typeof(MyHandler).Assembly);
    config.HandlerLifetime = ServiceLifetime.Scoped; // Default: Transient
});
```

### Duplicate Handler Detection

If reflection scanning finds more than one `IRequestHandler<,>` for the same request type, `MediatorConfiguration.DuplicateHandlerPolicy` controls what happens. It defaults to `Warning`.

```csharp
services.AddMediator(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(MyHandler).Assembly);
    cfg.DuplicateHandlerPolicy = RegistrationDiagnosticPolicy.Throw; // None | Warning (default) | Throw
});
```

* **`Warning`** (default) — logs via `ILogger` (resolved from DI; silently skipped if no logging is registered) and keeps the first handler scanned.
* **`Throw`** — throws `DuplicateHandlerException` synchronously inside `AddMediator`, before the service provider is built.
* **`None`** — ignores the condition; the first handler scanned wins, silently.

### Request Model & Missing Handler Detection

During IoC registration, assemblies can be scanned for request models (`IRequest` / `IRequest<TResponse>`). If a request model does not have a matching `IRequestHandler<,>` registered, `MediatorConfiguration.MissingHandlerPolicy` controls what happens:

```csharp
services.AddMediator(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(MyHandler).Assembly);
    cfg.MissingHandlerPolicy = RegistrationDiagnosticPolicy.Throw; // None | Warning (default) | Throw
});
```

* **`Warning`** (default) — scans request models and logs a warning via `ILogger` when the mediator is resolved.
* **`Throw`** — scans request models and throws `MissingHandlerException` synchronously inside `AddMediator`.
* **`None`** — completely bypasses request model scanning for fastest startup performance.

`RegistrationDiagnosticPolicy` is shared across registration-time diagnostics (`None` / `Warning` / `Throw`).

##  Why AnAspect.Mediator?

### 🚀 **Performance First**

* Optimized for minimal allocations and maximum throughput
* Consistent performance even with increasing handler count
* Lower memory footprint

### 🧩 **Elegant Type-Safe Architecture**

* Clean open generic support with compile-time safety
* Intuitive pipeline configuration
* No magic strings or runtime type discovery

### ⚡ **Flexible Pipeline**

* Fine-grained control over behavior execution
* Runtime pipeline modification
* Behavior grouping and exclusion
* Ordered execution with priority support



## License
This project is licensed under the MIT License.  
See the [LICENSE](LICENSE.txt) file for more details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.



## Author

Made with ❤️ by [Pouria7](https://github.com/Pouria7)

---

**NuGet Package**: [AnAspect.Mediator](https://www.nuget.org/packages/AnAspect.Mediator)
