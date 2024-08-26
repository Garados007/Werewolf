using System.Runtime.CompilerServices;

public static class Extensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static ConfiguredTaskAwaitable CAF(this Task task)
    {
        return task.ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static ConfiguredTaskAwaitable<T> CAF<T>(this Task<T> task)
    {
        return task.ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static ConfiguredValueTaskAwaitable CAF(this ValueTask task)
    {
        return task.ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static ConfiguredValueTaskAwaitable<T> CAF<T>(this ValueTask<T> task)
    {
        return task.ConfigureAwait(false);
    }
}
