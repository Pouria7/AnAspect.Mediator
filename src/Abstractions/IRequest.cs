namespace AnAspect.Mediator;

/// <summary>
/// Request that returns a response of type TResponse.
/// Users can derive custom interfaces from this (ICommand{T}, IQuery{T}, etc.)
/// </summary>
public interface IRequest<out TResponse>;

/// <summary>
/// Request without response (returns Unit).
/// </summary>
public interface IRequest : IRequest<Unit> { }
