using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Purifier.Autofac
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class CustomFunction
    {
        // Returning IComputed<TOut>

        public virtual Task<IComputed<TOut>> InvokeAsync<TIn, TOut>(
            Func<TIn, CancellationToken, Task<IComputed<TOut>>> fn,
            TIn input,
            CancellationToken cancellationToken = default)
            => fn.Invoke(input, cancellationToken);

        public virtual Task<IComputed<TOut>> InvokeAsync<TOut>(
            Func<CancellationToken, Task<IComputed<TOut>>> fn,
            CancellationToken cancellationToken = default)
            => fn.Invoke(cancellationToken);

        // Returning TOut  

        public virtual Task<TOut> InvokeAsync<TIn, TOut>(
            Func<TIn, CancellationToken, Task<TOut>> fn,
            TIn input,
            CancellationToken cancellationToken = default)
            => fn.Invoke(input, cancellationToken);

        public virtual Task<TOut> InvokeAsync<TOut>(
            Func<CancellationToken, Task<TOut>> fn,
            CancellationToken cancellationToken = default)
            => fn.Invoke(cancellationToken);
    }
}
