using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.Internal
{
    public sealed class FuncLiveState<T> : LiveState<T>
    {
        public Func<ILiveState<T>, CancellationToken, Task<T>> Computer { get; }

        public FuncLiveState(
            Options options,
            IServiceProvider services,
            Func<ILiveState<T>, CancellationToken, Task<T>> computer)
            : base(options, services, false)
        {
            Computer = computer;
            Initialize(options);
        }

        protected override Task<T> Compute(CancellationToken cancellationToken)
            => Computer.Invoke(this, cancellationToken);
    }
}
