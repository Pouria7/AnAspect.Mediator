
namespace AnAspect.Mediator.Tests.Core;


// Basic request/response
public record CreateUserCommand(string Name, string Email) : IRequest<UserDto>;
public record UserDto(Guid Id, string Name, string Email);

// Custom interfaces (ICommand, IQuery pattern)
public interface ICommand<out TResponse> : IRequest<TResponse> { }
public interface IQuery<out TResponse> : IRequest<TResponse> { }

public record GetUserQuery(Guid Id) : IQuery<UserDto?>;
public record DeleteUserCommand(Guid Id) : ICommand<bool>;

// Request without response
public record LogMessageCommand(string Message) : IRequest;

// For abstract base handler test
public record UpdateUserCommand(Guid Id, string Name) : IRequest<UserDto>;
