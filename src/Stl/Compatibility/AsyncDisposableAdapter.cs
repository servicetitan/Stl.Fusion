using System;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl.Compatibility
{
    public readonly struct AsyncDisposableAdapter<T> : IAsyncDisposable
#if !NETSTANDARD2_0
        where T : IAsyncDisposable?
#else
        where T : IDisposable?
#endif
    {
        public readonly T Target;

        public AsyncDisposableAdapter(T target)
            => Target = target;

        public ValueTask DisposeAsync()
        {
#if !NETSTANDARD2_0
            return Target?.DisposeAsync() ?? ValueTaskExt.CompletedTask;
#else
            if (Target is IAsyncDisposable ad)
                return ad.DisposeAsync();
            Target?.Dispose();
            return ValueTaskExt.CompletedTask;
#endif
        }
    }
}
