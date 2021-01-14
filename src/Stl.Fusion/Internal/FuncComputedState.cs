using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.Internal
{
    public sealed class FuncComputedState<T> : ComputedState<T>
    {
        public Func<IComputedState<T>, CancellationToken, Task<T>> Computer { get; }

        public FuncComputedState(
            Options options,
            IServiceProvider services,
            Func<IComputedState<T>, CancellationToken, Task<T>> computer,
            object? argument = null)
            : base(options, services, argument, false)
        {
            Computer = computer;
            Initialize(options);
        }

        protected override Task<T> ComputeValueAsync(CancellationToken cancellationToken)
            => Computer.Invoke(this, cancellationToken);
    }
}
