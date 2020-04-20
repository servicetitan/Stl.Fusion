using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Purifier.Autofac
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class CustomFunction
    {
        // Returning IComputed<TOut>

        public virtual ValueTask<IComputed<TOut>> Invoke<TIn, TOut>(
            Func<TIn, CancellationToken, ValueTask<IComputed<TOut>>> fn,
            TIn input,
            CancellationToken cancellationToken = default,
            ComputeContext? callOptions = null)
            => fn.Invoke(input, cancellationToken);

        public virtual ValueTask<IComputed<TOut>> Invoke<TOut>(
            Func<CancellationToken, ValueTask<IComputed<TOut>>> fn,
            CancellationToken cancellationToken = default,
            ComputeContext? callOptions = null)
            => fn.Invoke(cancellationToken);

        // Returning TOut  

        public virtual ValueTask<TOut> Invoke<TIn, TOut>(
            Func<TIn, CancellationToken, ValueTask<TOut>> fn,
            TIn input,
            CancellationToken cancellationToken = default,
            ComputeContext? callOptions = null)
            => fn.Invoke(input, cancellationToken);

        public virtual ValueTask<TOut> Invoke<TOut>(
            Func<CancellationToken, ValueTask<TOut>> fn,
            CancellationToken cancellationToken = default,
            ComputeContext? callOptions = null)
            => fn.Invoke(cancellationToken);
    }
}
