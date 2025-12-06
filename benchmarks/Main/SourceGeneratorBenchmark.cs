using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.Extensions.DependencyInjection;
using SGIMediator = Mediator.IMediator;

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
    public ValueTask<BenchmarkResponse> SourceGenerator_NoPipeline() =>
        _sourceGeneratorMediator.Send(_request);
}
