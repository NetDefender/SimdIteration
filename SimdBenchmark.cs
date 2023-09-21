using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace SimdIteration;

[MarkdownExporter]
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net48, baseline: true)]
[SimpleJob(RuntimeMoniker.Net70)]
[SimpleJob(RuntimeMoniker.Net80)]
public class SimdBenchmark
{
    private int[] _data;

    [Params(10000)]
    public int Size = 16;

    [GlobalSetup]
    public void Setup()
    {
        Random rnd = new Random();
        _data = new int[Size];
        for (int i = 0; i < _data.Length; i++)
        {
            _data[i] = rnd.Next(0, Size);
        }
    }

    [Benchmark]
    public (int Min, int Max) MinMaxSimd()
    {
#if NET48
        return (0, 0);
#else
        return _data.MinMaxSimd();
#endif
    }

    [Benchmark]
    public (int Min, int Max) MinMaxLinq()
    {
        return _data.MinMaxLinq();
    }

    [Benchmark]
    public (int Min, int Max) MinMaxForEach()
    {
        return _data.MinMaxForEach();
    }
}