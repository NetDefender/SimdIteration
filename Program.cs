using BenchmarkDotNet.Running;
using SimdIteration;
using static System.Console;

#if DEBUG
SimdBenchmark benchmark = new ();
benchmark.Setup();
WriteLine(benchmark.MinMaxLinq());
WriteLine(benchmark.MinMaxForEach());
WriteLine(benchmark.MinMaxSimd());
#else
BenchmarkRunner.Run<SimdBenchmark>();
#endif
