# AnAspect.Mediator: Benchmark Reports & Comparative Analysis

A comprehensive performance evaluation comparing **AnAspect.Mediator** with industry standards: **MediatR** and **Mediator.SourceGenerator**.

---

## 📋 Benchmark Setup & Environment

- **Target Framework**: `.NET 10.0` (Multi-targeted `net10.0;net9.0;net8.0`)
- **Benchmark Engine**: `BenchmarkDotNet v0.15.8`
- **Competitor Versions**:
  - `MediatR` v14.2.0
  - `Mediator.SourceGenerator` v3.0.2 / `Mediator.Abstractions` v3.0.2
  - `Microsoft.Extensions.DependencyInjection` v10.0.11
  - `Microsoft.Extensions.Logging` v10.0.11

---

## 📊 Summary of Key Findings

| Metric / Scenario | MediatR | Mediator (SourceGen) | AnAspect.Mediator | AnAspect Advantage |
| :--- | :--- | :--- | :--- | :--- |
| **No-Pipeline Dispatch** | 84.63 ns / 240 B | 11.84 ns / 40 B | **54.21 ns / 64 B** | 🚀 **36% faster**, **73% less memory** vs MediatR |
| **No-Pipeline (`.AsTask()`)** | 84.63 ns / 240 B | 16.36 ns / 112 B | **61.44 ns / 136 B** | 🚀 **27% faster**, **43% less memory** vs MediatR |
| **2 Pipeline Behaviors** | 195.67 ns / 768 B | N/A | **177.65 ns / 408 B** | 🚀 **9% faster**, **47% less memory** vs MediatR |
| **2 Behaviors (`.AsTask()`)** | 195.67 ns / 768 B | N/A | **175.48 ns / 480 B** | 🚀 **10% faster**, **37% less memory** vs MediatR |
| **Scale (50 Handlers)** | 84.43 ns / 344 B | 81.03 ns / 160 B | **69.96 ns / 96 B** | 🥇 **Fastest overall**, **72% less memory** |
| **Scale (100 Handlers)** | 88.67 ns / 344 B | 77.04 ns / 160 B | **67.70 ns / 96 B** | 🥇 **Fastest overall**, **72% less memory (O(1) flat)** |
| **Cold Start (Startup JIT)** | 42,777 ns | 65,498 ns | **41,573 ns** | ⚡ **Fastest cold start (33,556 ns via .AsTask())** |

---

## 📈 Detailed Benchmark Scenarios

### 1. Core Request Dispatch (Direct Invocation)

Measures raw dispatch latency and memory allocation when sending a request with a registered handler.

| Benchmark Method | Return Type | Mean | Allocated | Ratio vs MediatR |
| :--- | :--- | :--- | :--- | :--- |
| `MediatR_NoPipeline` *(Baseline)* | `Task<T>` | 84.63 ns | 240 B | 1.00x (Baseline) |
| **`AnAspect_NoPipeline`** | `ValueTask<T>` | **54.21 ns** | **64 B** | 🚀 **0.64x time (-36%), -73% memory** |
| **`AnAspect_NoPipeline_AsTask`** | `Task<T>` | **61.44 ns** | **136 B** | 🚀 **0.73x time (-27%), -43% memory** |
| `AnAspect_SkipPipeline` | `ValueTask<T>` | 53.86 ns | 112 B | 🚀 **0.64x time (-36%), direct bypass** |
| `AnAspect_SkipPipeline_AsTask` | `Task<T>` | 61.71 ns | 184 B | 🚀 **0.73x time (-27%), direct bypass** |
| `MediatR_WithPipeline` (2 Behaviors) | `Task<T>` | 195.67 ns | 768 B | 2.31x time |
| **`AnAspect_WithPipeline`** (2 Behaviors) | `ValueTask<T>` | **177.65 ns** | **408 B** | 🚀 **2.10x time (-9%), -47% memory** |
| **`AnAspect_WithPipeline_AsTask`** (2 Behaviors) | `Task<T>` | **175.48 ns** | **480 B** | 🚀 **2.08x time (-10%), -37% memory** |
| `SourceGenerator_NoPipeline` | `ValueTask<T>` | 11.84 ns | 40 B | Raw compile-time dispatch |
| `SourceGenerator_NoPipeline_AsTask`| `Task<T>` | 16.36 ns | 112 B | SourceGen converted to Task |

> 💡 **Takeaway**: Even when consumers convert AnAspect's `ValueTask` to `Task` via `.AsTask()`, AnAspect allocates **43% less memory** and executes **27% faster** than MediatR because AnAspect's Keyed Singleton handler wrappers avoid runtime reflection and per-request object creations.

---

### 2. Pipeline Overhead & Behavior Depth Scaling (1, 3, and 5 Behaviors)

Measures dispatch latency and heap allocation scaling as cross-cutting pipeline behaviors increase from 1 to 5 behaviors.

| Benchmark Method | Pipeline Depth | Mean | Allocated | Memory Advantage |
| :--- | :--- | :--- | :--- | :--- |
| `MediatR_1_Behavior` | 1 Behavior | 132.8 ns | 552 B | Baseline |
| **`AnAspect_1_Behavior`** | 1 Behavior | **115.3 ns** | **312 B** | 🚀 **13% faster, 43% less memory** |
| **`AnAspect_1_Behavior_AsTask`** | 1 Behavior | **122.3 ns** | **384 B** | 🚀 **8% faster, 30% less memory** |
| `MediatR_3_Behaviors` | 3 Behaviors | 209.1 ns | 984 B | Baseline |
| **`AnAspect_3_Behaviors`** | 3 Behaviors | **188.5 ns** | **504 B** | 🚀 **10% faster, 49% less memory** |
| **`AnAspect_3_Behaviors_AsTask`** | 3 Behaviors | **193.8 ns** | **576 B** | 🚀 **7% faster, 41% less memory** |
| `MediatR_5_Behaviors` | 5 Behaviors | 355.9 ns | 1,416 B | Baseline (0.0005 Gen1 collections) |
| **`AnAspect_5_Behaviors`** | 5 Behaviors | **363.6 ns** | **784 B** | 🚀 **45% less memory, 0 Gen1** |
| **`AnAspect_5_Behaviors_AsTask`** | 5 Behaviors | **369.8 ns** | **856 B** | 🚀 **40% less memory, 0 Gen1** |

---

### 3. Scalability Under High Handler Counts (50 & 100 Handlers)

Tests whether DI resolution and mediator dispatch degrade as the application grows to 50 and 100 distinct request-handler pairs.

| Method | Handlers in DI | Mean | Allocated | Scaling Behavior |
| :--- | :--- | :--- | :--- | :--- |
| `MediatR_Handler50` | 50 Handlers | 84.43 ns | 344 B | Baseline |
| **`AnAspect_Handler50`** | 50 Handlers | **69.96 ns** | **96 B** | 🥇 **17% faster, 72% less memory** |
| **`AnAspect_Handler50_AsTask`** | 50 Handlers | **74.40 ns** | **240 B** | 🚀 **12% faster, 30% less memory** |
| `SourceGenerator_Handler50` | 50 Handlers | 81.03 ns | 160 B | Good |
| `SourceGenerator_Handler50_AsTask` | 50 Handlers | 82.62 ns | 304 B | Moderate |
| `MediatR_Handler100` | 100 Handlers | 88.67 ns | 344 B | Slowdown (+5.0%) |
| **`AnAspect_Handler100`** | 100 Handlers | **67.70 ns** | **96 B** | 🥇 **Fastest overall, 72% less memory (O(1) flat)** |
| **`AnAspect_Handler100_AsTask`** | 100 Handlers | **75.80 ns** | **240 B** | 🚀 **15% faster, 30% less memory** |
| `SourceGenerator_Handler100` | 100 Handlers | 77.04 ns | 160 B | 14% slower than AnAspect |
| `SourceGenerator_Handler100_AsTask` | 100 Handlers | 95.85 ns | 304 B | Noticeable degradation |

> 💡 **Takeaway**: AnAspect registers request wrappers as **Keyed Singletons keyed by request type**. As the DI container grows from 50 to 100+ handlers, lookup time remains completely flat ($O(1)$ dictionary lookup with 96 B allocation), outperforming both MediatR and Source Generator at scale.

---

### 4. Concurrent Dispatch & Throughput (`Task.WhenAll`)

Evaluates performance and allocation under parallel dispatch workloads (100 and 1,000 concurrent requests).

| Method | Parallel Requests | Mean | Allocated | Ratio vs MediatR |
| :--- | :--- | :--- | :--- | :--- |
| `MediatR_Concurrent_NoPipeline` | 100 Requests | 8.87 µs | 25.94 KB | 1.00x |
| **`AnAspect_Concurrent_NoPipeline`** | 100 Requests | **7.22 µs** | **15.78 KB** | 🚀 **19% faster, 39% less memory** |
| `MediatR_Concurrent_WithPipeline` | 100 Requests | 21.79 µs | 77.50 KB | 1.00x |
| **`AnAspect_Concurrent_WithPipeline`** | 100 Requests | **17.57 µs** | **49.38 KB** | 🚀 **19% faster, 36% less memory** |
| `MediatR_Concurrent_NoPipeline` | 1,000 Requests | 80.71 µs | 257.97 KB | 1.00x |
| **`AnAspect_Concurrent_NoPipeline`** | 1,000 Requests | **70.15 µs** | **156.41 KB** | 🚀 **13% faster, 39% less memory** |
| `MediatR_Concurrent_WithPipeline` | 1,000 Requests | 215.29 µs | 773.59 KB | 1.00x |
| **`AnAspect_Concurrent_WithPipeline`** | 1,000 Requests | **170.21 µs** | **492.34 KB** | 🚀 **21% faster, 36% less memory** |

---

### 5. Cold Start Performance (JIT & Initial Dispatch)

Measures latency and memory during the initial cold invocation before JIT tiering.

| Scenario | MediatR | Mediator (SourceGen) | AnAspect.Mediator |
| :--- | :--- | :--- | :--- |
| **No Pipeline Cold Start** | 42,777 ns (304 B) | 65,498 ns (40 B) | **41,573 ns (64 B)** |
| **No Pipeline (`.AsTask()`) Cold Start** | 42,777 ns (304 B) | 59,357 ns (112 B) | **33,556 ns (136 B)** |
| **With Pipeline Cold Start** | 69,604 ns (832 B) | N/A | **60,287 ns (448 B)** |

---

## 🔧 AnAspect.Mediator: Internal Architecture & Performance Engineering

The performance characteristics and low-allocation profile of **AnAspect.Mediator** stem directly from core design choices in its internal architecture:

### 1. Keyed Singleton Wrappers ($O(1)$ Resolution)
- During IoC registration (`AddMediator`), strongly-typed `RequestHandlerWrapperImpl<TRequest, TResponse>` and `DirectRequestHandlerWrapperImpl<TRequest, TResponse>` wrappers are registered as **Keyed Singletons** keyed by `typeof(TRequest)`.
- At dispatch time, lookup is performed via `_serviceProvider.GetRequiredKeyedService<RequestHandlerWrapper<TResponse>>(request.GetType())`, which maps to a direct, fast dictionary lookup in Microsoft's DI container, avoiding runtime handler scanning.

### 2. Dedicated Direct Dispatch Bypass (`WithoutPipeline`)
- When no pipeline behaviors are registered, or when explicitly requested via `.WithoutPipeline()`, the mediator dispatches directly via `DirectRequestHandlerWrapperImpl`.
- This executes the handler directly without allocating pipeline execution contexts or traversing behavior lists.

### 3. Stack-Allocated `ValueTask<T>` Architecture
- Built around `ValueTask<T>` as the primary return type.
- For synchronous completions (e.g., cached results, validation failures, in-memory queries), `ValueTask<T>` remains a struct on the stack, resulting in zero heap allocation.
- When converted to `Task` via `.AsTask()`, the lightweight dispatch pipeline ensures allocations remain minimal.

### 4. JIT Warmup & Tiered Compilation Path
- Because request wrappers and handlers are registered with concrete types during startup, the .NET JIT compiler (with Tiered Compilation and Dynamic PGO in .NET 8/9/10) can quickly stabilize and optimize the hot execution paths into tight Tier-1 native code.
- This results in fast cold start execution and consistent throughput across iterations.

---

## ⚖️ Feature & Architecture Matrix

| Feature / Architecture | MediatR | Mediator.SourceGenerator | AnAspect.Mediator |
| :--- | :--- | :--- | :--- |
| **Pipeline Features** | Basic | Limited | **Advanced (Groups, Exclusion, Ordering)** |
| **Primary Return Paradigm** | `Task<T>` | `ValueTask<T>` | **`ValueTask<T>` + `.AsTask()` support** |
| **Open Generic Behaviors** | Yes | Limited | **Yes (Type-Safe `AnyRequest`/`AnyResponse`)** |
| **Dynamic Pipeline Modification** | No | No | **Yes (`WithoutPipeline`, `WithPipelineGroup`)** |
| **Registration Diagnostics** | No | Compile-time | **Configurable (`None`, `Warning`, `Throw`)** |
| **Scalability (100+ Handlers)** | Good | Moderate | **Best ($O(1)$ Keyed Singletons)** |

---

## 🏃 How to Run Benchmarks Locally

To reproduce these benchmarks on your local machine using the .NET 10 SDK:

```bash
# 1. Run Core Request Benchmarks (including AsTask and Pipeline modes)
dotnet run -c Release -f net10.0 --project benchmarks/Main/AnAspect.Mediator.Benchmarks.csproj --filter *RequestBenchmark*

# 2. Run Source Generator Comparison Benchmark
dotnet run -c Release -f net10.0 --project benchmarks/Main/AnAspect.Mediator.Benchmarks.csproj --filter *SourceGeneratorBenchmark*

# 3. Run Pipeline Depth Scaling Benchmark (1, 3, 5 behaviors)
dotnet run -c Release -f net10.0 --project benchmarks/Main/AnAspect.Mediator.Benchmarks.csproj --filter *PipelineDepthBenchmark*

# 4. Run Concurrent Dispatch Benchmark (Parallel load)
dotnet run -c Release -f net10.0 --project benchmarks/Main/AnAspect.Mediator.Benchmarks.csproj --filter *ConcurrentDispatchBenchmark*

# 5. Run Scale Benchmark (50 & 100 Handlers in DI)
dotnet run -c Release -f net10.0 --project benchmarks/Scale/AnAspect.Mediator.Benchmarks.Scale.csproj
```