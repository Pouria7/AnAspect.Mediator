namespace AnAspect.Mediator.Abstractions;

/// <summary>
/// Marker for any request type in open generic behaviors.
/// </summary>
public sealed class AnyRequest : IRequest<AnyResponse>
{
    private AnyRequest() { }
}

/// <summary>
/// Marker for any response type in open generic behaviors.
/// </summary>
public sealed class AnyResponse
{
    private AnyResponse() { }
}