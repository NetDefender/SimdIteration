using System.Runtime.CompilerServices;
namespace SimdIteration;

public sealed partial class IntConverter
{
	[MethodImpl(MethodImplOptions.Unmanaged)]
	public bool Unmanaged(int value)
	{
		return (value & ~value) == 0;
	}
	[MethodImpl(MethodImplOptions.NoInlining)]
	public bool NoInlining(int value)
	{
		return (value & ~value) == 0;
	}
	[MethodImpl(MethodImplOptions.ForwardRef)]
	public bool ForwardRef(int value)
	{
		return (value & ~value) == 0;
	}
	[MethodImpl(MethodImplOptions.Synchronized)]
	public bool Synchronized(int value)
	{
		return (value & ~value) == 0;
	}
	[MethodImpl(MethodImplOptions.NoOptimization)]
	public bool NoOptimization(int value)
	{
		return (value & ~value) == 0;
	}
	[MethodImpl(MethodImplOptions.PreserveSig)]
	public bool PreserveSig(int value)
	{
		return (value & ~value) == 0;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool AggressiveInlining(int value)
	{
		return (value & ~value) == 0;
	}
	[MethodImpl(MethodImplOptions.InternalCall)]
	public bool InternalCall(int value)
	{
		return (value & ~value) == 0;
	}
}