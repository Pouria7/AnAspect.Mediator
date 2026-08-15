using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using SGIMediator = Mediator.IMediator;

[assembly: MediatorOptions(ServiceLifetime = ServiceLifetime.Transient)]

namespace AnAspect.Mediator.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
[SimpleJob(runStrategy: RunStrategy.ColdStart)]
[SimpleJob()]
public class SourceGeneratorBenchmark
{
    private IMediator _anaspectNoPipeline = null!;

    private SGIMediator _sourceGeneratorMediator = null!;
    private BenchmarkRequest _request = null!;

    [GlobalSetup]
    public void Setup()
    {
        // AnAspect without pipeline
        var noPipeSvc = new ServiceCollection();
        noPipeSvc.AddMediator(typeof(BenchmarkHandler).Assembly);
        _anaspectNoPipeline = noPipeSvc.BuildServiceProvider()
            .GetRequiredService<IMediator>();


        // Mediator.SourceGenerator
        var sourceGenSvc = new ServiceCollection();
        sourceGenSvc.AddMediator();
        _sourceGeneratorMediator = sourceGenSvc.BuildServiceProvider()
            .GetRequiredService<SGIMediator>();

        _request = new BenchmarkRequest(Guid.NewGuid());
    }


    [Benchmark(Baseline = true)]
    public ValueTask<BenchmarkResponse> AnAspect_NoPipeline() =>
        _anaspectNoPipeline.SendAsync(_request);

    [Benchmark]
    public Task<BenchmarkResponse> AnAspect_NoPipeline_AsTask() =>
        _anaspectNoPipeline.SendAsync(_request).AsTask();

    [Benchmark]
    public ValueTask<BenchmarkResponse> SourceGenerator_NoPipeline() =>
        _sourceGeneratorMediator.Send(_request);

    [Benchmark]
    public Task<BenchmarkResponse> SourceGenerator_NoPipeline_AsTask() =>
        _sourceGeneratorMediator.Send(_request).AsTask();
}
