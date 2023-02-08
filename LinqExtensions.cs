using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SimdIteration;

internal static class LinqExtensions
{
    #region methods
    public static T[] MutateRef<T>(this T[] instance, Func<int, T, T> action)
        where T : struct, INumber<T>
    {
        int pos = 0;
        ref T item = ref MemoryMarshal.GetReference(new Span<T>(instance));
        ref T last = ref Unsafe.Add(ref item, instance.Length);

        while (Unsafe.IsAddressLessThan(ref item, ref last))
        {
            item = action(pos, item);
            pos = pos + 1;
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
    #endregion
}