using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if !NET48
using System.Runtime.Intrinsics;
#endif
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SimdIteration;

public static class LinqExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<TSource[]> OptimizedChunk<TSource>(this IEnumerable<TSource> source, int size)
    {
#if NET48
    if (source == null)
    {
        throw new ArgumentNullException(nameof(source));
    }
#else
        ArgumentNullException.ThrowIfNull(source);
#endif
#if NET8_0
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);
#else
        if(size < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(source));
        }
#endif
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
#if !NET48
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SumSimd(this int[] source) => SimdCore<int>.Sum(new ReadOnlySpan<int>(source));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (int Min, int Max) MinMaxSimd(this int[] source) => SimdCore<int>.MinMax(new ReadOnlySpan<int>(source));
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (int Min, int Max) MinMaxLinq(this int[] source)
    {
        return (source.Min(), source.Max());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (int Min, int Max) MinMaxForEach(this int[] source)
    {
        int min = source[0];
        int max = min;

        foreach(int value in source)
        {
            if(value < min)
            {
                min = value;
            }
            if(value > max)
            {
                max = value;
            }
        }

        return (min, max);
    }
}

#if !NET48
file static class SimdCore<T> where T : struct, INumber<T>
{
    #region fields
    private static readonly int _vectorLength;
#if NET8_0
    private static readonly bool _is512;
#endif
    private static readonly bool _is256;
    private static readonly bool _is128;
    #endregion

    #region ctor
    static SimdCore()
    {
#if NET8_0
        if (Vector512.IsHardwareAccelerated)
        {
            _is512 = true;
            _vectorLength = Vector512<T>.Count;
            return;
        }
#endif
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
    internal static T Sum(ReadOnlySpan<T> source)
    {
        if (source.Length > _vectorLength)
        {
#if NET8_0
            if (_is512)
            {
                return Sum512(source);
            }
#endif
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
#if NET8_0
    private static T Sum512(ReadOnlySpan<T> source)
    {
        T sum = T.Zero;
        Vector512<T> vectorSum512 = Vector512<T>.Zero;
        ref T begin512 = ref MemoryMarshal.GetReference(source);
        ref T last512 = ref Unsafe.Add(ref begin512, source.Length);
        ref T current512 = ref begin512;
        ref T to512 = ref Unsafe.Add(ref begin512, source.Length - _vectorLength);

        while (Unsafe.IsAddressLessThan(ref current512, ref to512))
        {
            vectorSum512 += Vector512.LoadUnsafe(ref current512);
            current512 = ref Unsafe.Add(ref current512, _vectorLength);
        }

        while (Unsafe.IsAddressLessThan(ref current512, ref last512))
        {
            unchecked
            {
                sum += current512;
            }
            current512 = ref Unsafe.Add(ref current512, 1);
        }

        return sum + Vector512.Sum(vectorSum512);
    }
#endif
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

    internal static (T Min, T Max) MinMax(ReadOnlySpan<T> source)
    {
        if (source.Length > _vectorLength)
        {
#if NET8_0
            if (_is512)
            {
                return MinMax512(source);
            }
#endif
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

#if NET8_0
    private static (T Min, T Max) MinMax512(ReadOnlySpan<T> source)
    {
        ref T current = ref MemoryMarshal.GetReference(source);
        ref T last = ref Unsafe.Add(ref current, source.Length);
        ref T to = ref Unsafe.Add(ref last, -_vectorLength);

        Vector512<T> minElement = Vector512.LoadUnsafe(ref current);
        Vector512<T> maxElement = minElement;
        current = ref Unsafe.Add(ref current, _vectorLength);

        while (Unsafe.IsAddressLessThan(ref current, ref to))
        {
            Vector512<T> tempElement = Vector512.LoadUnsafe(ref current);
            minElement = Vector512.Min(minElement, tempElement);
            maxElement = Vector512.Max(maxElement, tempElement);
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
#endif
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

    private static (T Min, T Max) MinMaxFallback(ReadOnlySpan<T> source)
    {
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
    #endregion
}
#endif