using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace SimdIteration;

public static class LinqExtensions
{
    public static T[] MutateRef<T>(this T[] instance, Func<int, T, T> action)
        where T : struct, INumber<T>
    {
        int pos = 0;
        ref T item = ref MemoryMarshal.GetReference(new Span<T>(instance));
        ref T last = ref Unsafe.Add(ref item, instance.Length);

        while (Unsafe.IsAddressLessThan(ref item, ref last))
        {
            item = action(pos, item);
            pos++;
            item = ref Unsafe.Add(ref item, 1);
        }

        return instance;
    }

    public static T[] MutateClassic<T>(this T[] instance, Func<int, T, T> action)
    {
        for (int i = 0; i < instance.Length; i++)
        {
            instance[i] = action(i, instance[i]);
        }

        return instance;
    }

    public static IEnumerable<TSource[]> OptimizedChunk<TSource>(this IEnumerable<TSource> source, int size)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);
        return SafeOptimizedChunker(source, size);
    }

    private static IEnumerable<TSource[]> SafeOptimizedChunker<TSource>(IEnumerable<TSource> source, int size)
    {
        if (source is TSource[] array)
        {
            for (int start = 0; start < array.Length; start += size)
            {
                int end = start + size;
                if (end >= array.Length)
                {
                    end = array.Length;
                }
                TSource[] chunk = new TSource[end - start];
                for (int sourceIndex = start, chunkIndex = 0; sourceIndex < end; sourceIndex++, chunkIndex++)
                {
                    chunk[chunkIndex] = array[sourceIndex];
                }
                yield return chunk;
            }
        }
        else if (source is IList<TSource> list)
        {
            for (int start = 0; start < list.Count; start += size)
            {
                int end = start + size;
                if (end >= list.Count)
                {
                    end = list.Count;
                }
                TSource[] chunk = new TSource[end - start];
                for (int sourceIndex = start, chunkIndex = 0; sourceIndex < end; sourceIndex++, chunkIndex++)
                {
                    chunk[chunkIndex] = list[sourceIndex];
                }
                yield return chunk;
            }
        }
        else
        {
            using IEnumerator<TSource> iterator = source.GetEnumerator();
            while (iterator.MoveNext())
            {
                TSource[] chunk = new TSource[size];
                chunk[0] = iterator.Current;
                int i = 1;
                for (; i < chunk.Length && iterator.MoveNext(); i++)
                {
                    chunk[i] = iterator.Current;
                }
                if (i < size)
                {
                    Array.Resize(ref chunk, i);
                }
                yield return chunk;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Sum(this int[] source) => SimdCore<int>.Sum(new ReadOnlySpan<int>(source));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (int Min, int Max) MinMax(this int[] source) => SimdCore<int>.MinMax(new ReadOnlySpan<int>(source));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal Average(this int[] source) => SimdCore<int>.Sum(new ReadOnlySpan<int>(source)) / (source.Length * 1M);
}

file static class SimdCore<T> where T : struct, INumber<T>
{
    #region fields
    private static readonly int _vectorLength;
    private static readonly bool _is256;
    private static readonly bool _is128;
    #endregion

    #region ctor
    static SimdCore()
    {
        if (Vector256.IsHardwareAccelerated)
        {
            _is256 = true;
            _vectorLength = Vector256<T>.Count;
            return;
        }
        if (Vector128.IsHardwareAccelerated)
        {
            _is128 = true;
            _vectorLength = Vector128<T>.Count;
            return;
        }
        _vectorLength = int.MaxValue;
    }
    #endregion

    #region methods
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T Sum(ReadOnlySpan<T> source)
    {
        if (source.Length > _vectorLength)
        {
            if (_is256)
            {
                return Sum256(source);
            }
            if (_is128)
            {
                return Sum128(source);
            }
        }
        return SumFallback(source);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T SumFallback(ReadOnlySpan<T> source)
    {
        T sum = T.Zero;
        unchecked
        {
            for (int i = 0; i < source.Length; i++)
            {
                sum += source[i];
            }
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T Sum128(ReadOnlySpan<T> source)
    {
        T sum = T.Zero;
        Vector128<T> vectorSum128 = Vector128<T>.Zero;
        ref T begin128 = ref MemoryMarshal.GetReference(source);
        ref T last128 = ref Unsafe.Add(ref begin128, source.Length);
        ref T current128 = ref begin128;
        ref T to128 = ref Unsafe.Add(ref begin128, source.Length - _vectorLength);

        while (Unsafe.IsAddressLessThan(ref current128, ref to128))
        {
            vectorSum128 += Vector128.LoadUnsafe(ref current128);
            current128 = ref Unsafe.Add(ref current128, _vectorLength);
        }

        while (Unsafe.IsAddressLessThan(ref current128, ref last128))
        {
            unchecked
            {
                sum += current128;
            }
            current128 = ref Unsafe.Add(ref current128, 1);
        }

        return sum + Vector128.Sum(vectorSum128);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T Sum256(ReadOnlySpan<T> source)
    {
        T sum = T.Zero;
        Vector256<T> vectorSum256 = Vector256<T>.Zero;
        ref T begin256 = ref MemoryMarshal.GetReference(source);
        ref T last256 = ref Unsafe.Add(ref begin256, source.Length);
        ref T current256 = ref begin256;
        ref T to256 = ref Unsafe.Add(ref begin256, source.Length - _vectorLength);

        while (Unsafe.IsAddressLessThan(ref current256, ref to256))
        {
            vectorSum256 += Vector256.LoadUnsafe(ref current256);
            current256 = ref Unsafe.Add(ref current256, _vectorLength);
        }

        while (Unsafe.IsAddressLessThan(ref current256, ref last256))
        {
            unchecked
            {
                sum += current256;
            }
            current256 = ref Unsafe.Add(ref current256, 1);
        }

        return sum + Vector256.Sum(vectorSum256);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (T Min, T Max) MinMax(ReadOnlySpan<T> source)
    {
        if (source.Length > _vectorLength)
        {
            if (_is256)
            {
                return MinMax256(source);
            }
            if (_is128)
            {
                return MinMax128(source);
            }
        }
        return MinMaxFallback(source);
    }

    private static (T Min, T Max) MinMax256(ReadOnlySpan<T> source)
    {
        ref T current = ref MemoryMarshal.GetReference(source);
        ref T last = ref Unsafe.Add(ref current, source.Length);
        ref T to = ref Unsafe.Add(ref last, -_vectorLength);

        Vector256<T> minElement = Vector256.LoadUnsafe(ref current);
        Vector256<T> maxElement = minElement;
        current = ref Unsafe.Add(ref current, _vectorLength);

        while (Unsafe.IsAddressLessThan(ref current, ref to))
        {
            Vector256<T> tempElement = Vector256.LoadUnsafe(ref current);
            minElement = Vector256.Min(minElement, tempElement);
            maxElement = Vector256.Max(maxElement, tempElement);
            current = ref Unsafe.Add(ref current, _vectorLength);
        }

        T min = minElement[0];
        T max = maxElement[0];

        for (int i = 1; i < _vectorLength; i++)
        {
            T tempMin = minElement[i];
            if (tempMin < min)
            {
                min = tempMin;
            }
            T tempMax = maxElement[i];
            if (tempMax > max)
            {
                max = tempMax;
            }
        }

        while (Unsafe.IsAddressLessThan(ref current, ref last))
        {
            if (current < min)
            {
                min = current;
            }
            if (current > max)
            {
                max = current;
            }
            current = ref Unsafe.Add(ref current, 1);
        }

        return (min, max);
    }

    private static (T Min, T Max) MinMaxFallback(ReadOnlySpan<T> source)
    {
        if (source.Length == 0)
        {
            ThrowNoElements();
        }

        T min = source[0];
        T max = min;

        for (int i = 1; i < source.Length; i++)
        {
            T current = source[i];
            if (current < min)
            {
                min = current;
            }
            if (current > max)
            {
                max = current;
            }
        }

        return (min, max);
    }

    private static (T Min, T Max) MinMax128(ReadOnlySpan<T> source)
    {
        ref T current = ref MemoryMarshal.GetReference(source);
        ref T last = ref Unsafe.Add(ref current, source.Length);
        ref T to = ref Unsafe.Add(ref last, -_vectorLength);

        Vector128<T> minElement = Vector128.LoadUnsafe(ref current);
        Vector128<T> maxElement = minElement;
        current = ref Unsafe.Add(ref current, _vectorLength);

        while (Unsafe.IsAddressLessThan(ref current, ref to))
        {
            Vector128<T> tempElement = Vector128.LoadUnsafe(ref current);
            minElement = Vector128.Min(minElement, tempElement);
            maxElement = Vector128.Max(maxElement, tempElement);
            current = ref Unsafe.Add(ref current, _vectorLength);
        }

        T min = minElement[0];
        T max = maxElement[0];

        for (int i = 1; i < _vectorLength; i++)
        {
            T tempMin = minElement[i];
            if (tempMin < min)
            {
                min = tempMin;
            }
            T tempMax = maxElement[i];
            if (tempMax > max)
            {
                max = tempMax;
            }
        }

        while (Unsafe.IsAddressLessThan(ref current, ref last))
        {
            if (current < min)
            {
                min = current;
            }
            if (current > max)
            {
                max = current;
            }
            current = ref Unsafe.Add(ref current, 1);
        }

        return (min, max);
    }

    [DoesNotReturn]
    static void ThrowNoElements()
    {
        throw new InvalidOperationException("Source contains no elements");
    }
    #endregion
}