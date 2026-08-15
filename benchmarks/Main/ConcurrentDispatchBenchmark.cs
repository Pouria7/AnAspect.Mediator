using AnAspect.Mediator;
using AnAspect.Mediator.Abstractions;
using AnAspect.Mediator.Registration;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace AnAspect.Mediator.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class ConcurrentDispatchBenchmark
{
    private IMediator _anaspectNoPipeline = null!;
    private IMediator _anaspectWithPipeline = null!;

    private MediatR.IMediator _mediatrNoPipeline = null!;
    private MediatR.IMediator _mediatrWithPipeline = null!;

    private BenchmarkRequest[] _requests = null!;

    [Params(100, 1000)]
    public int ConcurrencyCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // AnAspect without pipeline
        var noPipeSvc = new ServiceCollection();
        noPipeSvc.AddMediator(typeof(BenchmarkHandler).Assembly);
        _anaspectNoPipeline = noPipeSvc.BuildServiceProvider().GetRequiredService<IMediator>();

        // AnAspect with pipeline
        var pipeSvc = new ServiceCollection();
        pipeSvc.AddMediator((MediatorConfiguration cfg) =>
        {
            cfg.RegisterServicesFromAssembly(typeof(BenchmarkHandler).Assembly);
            cfg.AddBehavior<NoOpBehavior, BenchmarkRequest, BenchmarkResponse>(order: 1);
            cfg.AddBehavior<NoOpBehavior2, BenchmarkRequest, BenchmarkResponse>(order: 2);
        });
        _anaspectWithPipeline = pipeSvc.BuildServiceProvider().GetRequiredService<IMediator>();

        // MediatR without pipeline
        var mediatrNoPipeSvc = new ServiceCollection();
        mediatrNoPipeSvc.AddLogging();
        mediatrNoPipeSvc.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(MediatRBenchmarkHandler).Assembly));
        _mediatrNoPipeline = mediatrNoPipeSvc.BuildServiceProvider().GetRequiredService<MediatR.IMediator>();

        // MediatR with pipeline
        var mediatrPipeSvc = new ServiceCollection();
        mediatrPipeSvc.AddLogging();
        mediatrPipeSvc.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(MediatRBenchmarkHandler).Assembly);
            cfg.AddBehavior<MediatRNoOpBehavior>();
            cfg.AddBehavior<MediatRNoOpBehavior2>();
        });
        _mediatrWithPipeline = mediatrPipeSvc.BuildServiceProvider().GetRequiredService<MediatR.IMediator>();

        _requests = Enumerable.Range(0, 1000)
            .Select(_ => new BenchmarkRequest(Guid.NewGuid()))
            .ToArray();
    }

    [Benchmark(Baseline = true)]
    public async Task MediatR_Concurrent_NoPipeline()
    {
        var tasks = new Task<BenchmarkResponse>[ConcurrencyCount];
        for (int i = 0; i < ConcurrencyCount; i++)
        {
            tasks[i] = _mediatrNoPipeline.Send(_requests[i]);
        }
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task MediatR_Concurrent_WithPipeline()
    {
        var tasks = new Task<BenchmarkResponse>[ConcurrencyCount];
        for (int i = 0; i < ConcurrencyCount; i++)
        {
            tasks[i] = _mediatrWithPipeline.Send(_requests[i]);
        }
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task AnAspect_Concurrent_NoPipeline()
    {
        var tasks = new Task<BenchmarkResponse>[ConcurrencyCount];
        for (int i = 0; i < ConcurrencyCount; i++)
        {
            tasks[i] = _anaspectNoPipeline.SendAsync(_requests[i]).AsTask();
        }
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task AnAspect_Concurrent_WithPipeline()
    {
        var tasks = new Task<BenchmarkResponse>[ConcurrencyCount];
        for (int i = 0; i < ConcurrencyCount; i++)
        {
            tasks[i] = _anaspectWithPipeline.SendAsync(_requests[i]).AsTask();
        }
        await Task.WhenAll(tasks);
    }
}
