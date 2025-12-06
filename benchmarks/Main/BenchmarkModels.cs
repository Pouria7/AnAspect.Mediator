public record BenchmarkRequest(Guid Id)
    : AnAspect.Mediator.IRequest<BenchmarkResponse>,
      MediatR.IRequest<BenchmarkResponse>,
      Mediator.IRequest<BenchmarkResponse>; 

public record BenchmarkResponse(Guid Id, string Data);