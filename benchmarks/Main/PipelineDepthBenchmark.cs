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
[SimpleJob]
public class PipelineDepthBenchmark
{
    private IMediator _anaspect1 = null!;
    private IMediator _anaspect3 = null!;
    private IMediator _anaspect5 = null!;

    private MediatR.IMediator _mediatr1 = null!;
    private MediatR.IMediator _mediatr3 = null!;
    private MediatR.IMediator _mediatr5 = null!;

    private BenchmarkRequest _request = null!;

    [GlobalSetup]
    public void Setup()
    {
        _request = new BenchmarkRequest(Guid.NewGuid());

        // AnAspect - 1 Behavior
        var a1Svc = new ServiceCollection();
        a1Svc.AddMediator((MediatorConfiguration cfg) =>
        {
            cfg.RegisterServicesFromAssembly(typeof(BenchmarkHandler).Assembly);
            cfg.AddBehavior<NoOpBehavior, BenchmarkRequest, BenchmarkResponse>(order: 1);
        });
        _anaspect1 = a1Svc.BuildServiceProvider().GetRequiredService<IMediator>();

        // AnAspect - 3 Behaviors
        var a3Svc = new ServiceCollection();
        a3Svc.AddMediator((MediatorConfiguration cfg) =>
        {
            cfg.RegisterServicesFromAssembly(typeof(BenchmarkHandler).Assembly);
            cfg.AddBehavior<NoOpBehavior, BenchmarkRequest, BenchmarkResponse>(order: 1);
            cfg.AddBehavior<NoOpBehavior2, BenchmarkRequest, BenchmarkResponse>(order: 2);
            cfg.AddBehavior<NoOpBehavior3, BenchmarkRequest, BenchmarkResponse>(order: 3);
        });
        _anaspect3 = a3Svc.BuildServiceProvider().GetRequiredService<IMediator>();

        // AnAspect - 5 Behaviors
        var a5Svc = new ServiceCollection();
        a5Svc.AddMediator((MediatorConfiguration cfg) =>
        {
            cfg.RegisterServicesFromAssembly(typeof(BenchmarkHandler).Assembly);
            cfg.AddBehavior<NoOpBehavior, BenchmarkRequest, BenchmarkResponse>(order: 1);
            cfg.AddBehavior<NoOpBehavior2, BenchmarkRequest, BenchmarkResponse>(order: 2);
            cfg.AddBehavior<NoOpBehavior3, BenchmarkRequest, BenchmarkResponse>(order: 3);
            cfg.AddBehavior<NoOpBehavior4, BenchmarkRequest, BenchmarkResponse>(order: 4);
            cfg.AddBehavior<NoOpBehavior5, BenchmarkRequest, BenchmarkResponse>(order: 5);
        });
        _anaspect5 = a5Svc.BuildServiceProvider().GetRequiredService<IMediator>();

        // MediatR - 1 Behavior
        var m1Svc = new ServiceCollection();
        m1Svc.AddLogging();
        m1Svc.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(MediatRBenchmarkHandler).Assembly);
            cfg.AddBehavior<MediatRNoOpBehavior>();
        });
        _mediatr1 = m1Svc.BuildServiceProvider().GetRequiredService<MediatR.IMediator>();

        // MediatR - 3 Behaviors
        var m3Svc = new ServiceCollection();
        m3Svc.AddLogging();
        m3Svc.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(MediatRBenchmarkHandler).Assembly);
            cfg.AddBehavior<MediatRNoOpBehavior>();
            cfg.AddBehavior<MediatRNoOpBehavior2>();
            cfg.AddBehavior<MediatRNoOpBehavior3>();
        });
        _mediatr3 = m3Svc.BuildServiceProvider().GetRequiredService<MediatR.IMediator>();

        // MediatR - 5 Behaviors
        var m5Svc = new ServiceCollection();
        m5Svc.AddLogging();
        m5Svc.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(MediatRBenchmarkHandler).Assembly);
            cfg.AddBehavior<MediatRNoOpBehavior>();
            cfg.AddBehavior<MediatRNoOpBehavior2>();
            cfg.AddBehavior<MediatRNoOpBehavior3>();
            cfg.AddBehavior<MediatRNoOpBehavior4>();
            cfg.AddBehavior<MediatRNoOpBehavior5>();
        });
        _mediatr5 = m5Svc.BuildServiceProvider().GetRequiredService<MediatR.IMediator>();
    }

    // 1 Behavior
    [Benchmark(Baseline = true)]
    public Task<BenchmarkResponse> MediatR_1_Behavior() => _mediatr1.Send(_request);

    [Benchmark]
    public ValueTask<BenchmarkResponse> AnAspect_1_Behavior() => _anaspect1.SendAsync(_request);

    [Benchmark]
    public Task<BenchmarkResponse> AnAspect_1_Behavior_AsTask() => _anaspect1.SendAsync(_request).AsTask();

    // 3 Behaviors
    [Benchmark]
    public Task<BenchmarkResponse> MediatR_3_Behaviors() => _mediatr3.Send(_request);

    [Benchmark]
    public ValueTask<BenchmarkResponse> AnAspect_3_Behaviors() => _anaspect3.SendAsync(_request);

    [Benchmark]
    public Task<BenchmarkResponse> AnAspect_3_Behaviors_AsTask() => _anaspect3.SendAsync(_request).AsTask();

    // 5 Behaviors
    [Benchmark]
    public Task<BenchmarkResponse> MediatR_5_Behaviors() => _mediatr5.Send(_request);

    [Benchmark]
    public ValueTask<BenchmarkResponse> AnAspect_5_Behaviors() => _anaspect5.SendAsync(_request);

    [Benchmark]
    public Task<BenchmarkResponse> AnAspect_5_Behaviors_AsTask() => _anaspect5.SendAsync(_request).AsTask();
}
