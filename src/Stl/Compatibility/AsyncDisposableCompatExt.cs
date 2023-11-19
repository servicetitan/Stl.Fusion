using Stl.Compatibility;

// ReSharper disable once CheckNamespace
namespace System;

public static class AsyncDisposableCompatExt
{
    /// <summary>
    /// Use only to avoid conditional compilation directives
    /// in parts where component in one of legacy targets
    /// does not supports <see cref="IAsyncDisposable"/>.
    /// </summary>
    /// <typeparam name="T">The type of disposable.</typeparam>
    /// <param name="target"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AsyncDisposableAdapter<T> ToAsyncDisposableAdapter<T>(this T target)
#if !NETSTANDARD2_0
        where T : IAsyncDisposable?
#else
        where T : IDisposable?
#endif
        => new(target);
}
