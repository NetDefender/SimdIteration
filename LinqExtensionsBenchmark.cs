using BenchmarkDotNet.Attributes;

namespace SimdIteration;

public class LinqExtensionsBenchmark
{
    private int[] _data;
    private Func<int, int, int> _mutation;

    [Params( 100000)]
    public int LEN;

    [GlobalSetup]
    public void Setup()
    {
        _data = new int[LEN];
        Random rnd = new(Environment.TickCount);
        for (int i = 0; i < LEN; i++)
        {
            _data[i] = rnd.Next(0, int.MaxValue);
        }
        _mutation = (i, value) => i;
    }

    [Benchmark(Baseline = true)]
    public int[] ClassicArrayMutation()
    {
        LinqExtensions.MutateClassic(_data, _mutation);
        return _data;
    }

    [Benchmark]
    public int[] RefArrayMutation()
    {
        LinqExtensions.MutateRef(_data, _mutation);
        return _data;
    }
}
