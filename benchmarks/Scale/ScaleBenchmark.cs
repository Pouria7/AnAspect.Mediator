using BenchmarkDotNet.Attributes;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using ISgMediator = Mediator.IMediator;


[assembly: MediatorOptions(Namespace = "AnAspect.Mediator.Benchmarks.Scale", ServiceLifetime = ServiceLifetime.Transient)]

namespace AnAspect.Mediator.Benchmarks.Scale;


[MemoryDiagnoser]
public class ScaleBenchmark
{
    private ServiceProvider _anaspectServiceProvider = null!;
    private ServiceProvider _mediatrServiceProvider = null!;
    private ServiceProvider _sgServiceProvider = null!;

    private AnAspect.Mediator.IMediator _anaspectMediator = null!;
    private MediatR.IMediator _mediatrMediator = null!;
    private ISgMediator _sgMediator = null!;

    private readonly Guid _testId = Guid.NewGuid();

    [GlobalSetup]
    public void Setup()
    {
        // Setup AnAspect.Mediator
        var anaspectServices = new ServiceCollection();
        anaspectServices.AddMediator(typeof(ScaleBenchmark).Assembly);
        _anaspectServiceProvider = anaspectServices.BuildServiceProvider();
        _anaspectMediator = _anaspectServiceProvider.GetRequiredService<AnAspect.Mediator.IMediator>();

        // Setup MediatR
        var mediatrServices = new ServiceCollection();
        mediatrServices.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ScaleBenchmark).Assembly));
        _mediatrServiceProvider = mediatrServices.BuildServiceProvider();
        _mediatrMediator = _mediatrServiceProvider.GetRequiredService<MediatR.IMediator>();

        // Setup SourceGenerator
        var sgServices = new ServiceCollection();
        Microsoft.Extensions.DependencyInjection.MediatorDependencyInjectionExtensions.AddMediator(sgServices);

        _sgServiceProvider = sgServices.BuildServiceProvider();
        _sgMediator = _sgServiceProvider.GetRequiredService<ISgMediator>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _anaspectServiceProvider?.Dispose();
        _mediatrServiceProvider?.Dispose();
        _sgServiceProvider?.Dispose();
    }

    // Testing handler #50 (middle)
    [Benchmark(Baseline = true)]
    public async Task<ScaleBenchmarkResponse> MediatR_Handler50()
    {
        return await _mediatrMediator.Send(new BenchmarkRequest50(_testId));
    }

    [Benchmark]
    public async ValueTask<ScaleBenchmarkResponse> AnAspect_Handler50()
    {
        return await _anaspectMediator.SendAsync(new BenchmarkRequest50(_testId));
    }

    [Benchmark]
    public async ValueTask<ScaleBenchmarkResponse> SourceGenerator_Handler50()
    {
        return await _sgMediator.Send(new BenchmarkRequest50(_testId));
    }

    // Testing handler #100 (last)
    [Benchmark]
    public async Task<ScaleBenchmarkResponse> MediatR_Handler100()
    {
        return await _mediatrMediator.Send(new BenchmarkRequest100(_testId));
    }

    [Benchmark]
    public async ValueTask<ScaleBenchmarkResponse> AnAspect_Handler100()
    {
        return await _anaspectMediator.SendAsync(new BenchmarkRequest100(_testId));
    }

    [Benchmark]
    public async ValueTask<ScaleBenchmarkResponse> SourceGenerator_Handler100()
    {
        return await _sgMediator.Send(new BenchmarkRequest100(_testId));
    }
}
