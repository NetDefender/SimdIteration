using BenchmarkDotNet.Running;
using SimdIteration;

#if DEBUG
LinqExtensionsBenchmark benchmark = new LinqExtensionsBenchmark();
benchmark.LEN = 10;
benchmark.Setup();
benchmark.ClassicArrayMutation();
benchmark.RefArrayMutation();
#else
BenchmarkRunner.Run<LinqExtensionsBenchmark>();
#endif