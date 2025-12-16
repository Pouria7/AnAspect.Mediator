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
    .ExcludeBehavior<IPerformanceMonitoringBehavior, AnyRequest, AnyResponse>()
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

##  Performance Benchmarks

AnAspect.Mediator is engineered for maximum performance and minimal memory allocation. Benchmark results show significant advantages over both MediatR and source generator-based solutions.

### 🥇 Scalability (50-100 Handlers)

| Method | Mean | Allocated | Performance Advantage |
|--------|------|-----------|----------------------|
| **AnAspect (50 handlers)** | **87.66 ns** | **96 B** | ✅ **23% faster** than MediatR<br>✅ **3% faster** than Source Generator |
| MediatR (50 handlers) | 114.14 ns | 344 B | Baseline |
| SourceGenerator (50 handlers) | 90.67 ns | 160 B | - |
| **AnAspect (100 handlers)** | **88.14 ns** | **96 B** | ✅ **25% faster** than MediatR<br>✅ **19% faster** than Source Generator |
| MediatR (100 handlers) | 117.07 ns | 344 B | Baseline |
| SourceGenerator (100 handlers) | 109.24 ns | 160 B | - |

> **Key Insight**: AnAspect maintains consistent performance even as handler count increases.

### 🏆 Main Performance Comparison

| Method | Mean | Allocated | Performance vs MediatR |
|--------|------|-----------|------------------------|
| **AnAspect (No Pipeline)** | **54.92 ns** | **64 B** | 🚀 **1.8x faster** |
| MediatR (No Pipeline) | 92.60 ns | 240 B | Baseline |
| **AnAspect (With Pipeline)** | **173.71 ns** | **344 B** | 🚀 **1.3x faster** |
| MediatR (With Pipeline) | 227.68 ns | 768 B | Baseline |

### ⚡ Cold Start Performance

| Method | Mean | Allocated | 
|--------|------|-----------|
| **AnAspect (No Pipeline)** | **39,525 ns** | **64 B** |
| MediatR (No Pipeline) | 56,695 ns | 304 B |
| **AnAspect (With Pipeline)** | **74,614 ns** | **384 B** |
| MediatR (With Pipeline) | 74,402 ns | 832 B |


### 📊 Performance Summary

1. **Memory Efficiency**: **72% less allocation** than MediatR
2. **Scalability**: **Outperforms both MediatR and Source Generator** with 50+ handlers
3. **Execution Speed**: **1.3x-1.8x faster** than MediatR in production scenarios
4. **Cold Start**: **35% faster** than Source Generator alternatives
5. **Pipeline Overhead**: Minimal performance impact with pipeline enabled

* [Performance Results](./benchmarks/README.md)

## Configuration

```csharp
services.AddMediator(config => 
{
    config.RegisterServicesFromAssembly(typeof(MyHandler).Assembly);
    config.HandlerLifetime = ServiceLifetime.Scoped; // Default: Transient
});
```

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
See the [LICENSE](LICENSE) file for more details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.



## Author

Made with ❤️ by [Pouria7](https://github.com/Pouria7)

---

**NuGet Package**: [AnAspect.Mediator](https://www.nuget.org/packages/AnAspect.Mediator)
