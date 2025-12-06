using AnAspect.Mediator;
using AnAspect.Mediator.Abstractions;
using AnAspect.Mediator.Registration;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.Extensions.DependencyInjection;

namespace AnAspect.Mediator.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
[SimpleJob(runStrategy: RunStrategy.ColdStart)]
[SimpleJob()]
public class RequestBenchmark
{
    private IMediator _anaspectNoPipeline = null!;
    private IMediator _anaspectWithPipeline = null!;

    private MediatR.IMediator _mediatrNoPipeline = null!;
    private MediatR.IMediator _mediatrWithPipeline = null!;
    private BenchmarkRequest _request = null!;

    [GlobalSetup]
    public void Setup()
    {
        // AnAspect without pipeline
        var noPipeSvc = new ServiceCollection();
        noPipeSvc.AddMediator(typeof(BenchmarkHandler).Assembly);
        _anaspectNoPipeline = noPipeSvc.BuildServiceProvider()
            .GetRequiredService<IMediator>();

        // AnAspect with pipeline
        var pipeSvc = new ServiceCollection();
        pipeSvc.AddMediator((MediatorConfiguration cfg) =>
        {
            cfg.RegisterServicesFromAssembly(typeof(BenchmarkHandler).Assembly);
            cfg.AddBehavior<IPipelineBehavior<AnyRequest, AnyResponse>, AnyRequest, AnyResponse>(order: 1);
        });
        _anaspectWithPipeline = pipeSvc.BuildServiceProvider()
            .GetRequiredService<IMediator>();

        // MediatR without pipeline
        var mediatrNoPipeSvc = new ServiceCollection();
        mediatrNoPipeSvc.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(MediatRBenchmarkHandler).Assembly));
        _mediatrNoPipeline = mediatrNoPipeSvc.BuildServiceProvider()
            .GetRequiredService<MediatR.IMediator>();

        // MediatR with pipeline
        var mediatrPipeSvc = new ServiceCollection();
        mediatrPipeSvc.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(MediatRBenchmarkHandler).Assembly);
            cfg.AddBehavior<MediatRNoOpBehavior>();
            cfg.AddBehavior<MediatRNoOpBehavior2>();
        });
        _mediatrWithPipeline = mediatrPipeSvc.BuildServiceProvider()
            .GetRequiredService<MediatR.IMediator>();


        _request = new BenchmarkRequest(Guid.NewGuid());
    }

    [Benchmark(Baseline = true)]
    public Task<BenchmarkResponse> MediatR_NoPipeline() =>
        _mediatrNoPipeline.Send(_request);

    [Benchmark]
    public Task<BenchmarkResponse> MediatR_WithPipeline() =>
        _mediatrWithPipeline.Send(_request);

    [Benchmark]
    public ValueTask<BenchmarkResponse> AnAspect_NoPipeline() =>
        _anaspectNoPipeline.SendAsync(_request);


    [Benchmark]
    public ValueTask<BenchmarkResponse> AnAspect_SkipPipeline() =>
        _anaspectWithPipeline.WithoutPipeline().SendAsync(_request);

    [Benchmark]
    public ValueTask<BenchmarkResponse> AnAspect_WithPipeline() =>
        _anaspectWithPipeline.SendAsync(_request);


}
