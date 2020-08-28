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
            IServiceProvider serviceProvider,
            Func<ILiveState<T>, CancellationToken, Task<T>> computer,
            object? argument = null)
            : base(options, serviceProvider, argument, false)
        {
            Computer = computer;
            Initialize(options);
        }

        protected override Task<T> ComputeValueAsync(CancellationToken cancellationToken)
            => Computer.Invoke(this, cancellationToken);
    }

    public sealed class FuncLiveState<T, TOwn> : LiveState<T, TOwn>
    {
        public Func<ILiveState<T, TOwn>, CancellationToken, Task<T>> Computer { get; }

        public FuncLiveState(
            Options options,
            IServiceProvider serviceProvider,
            Func<ILiveState<T, TOwn>, CancellationToken, Task<T>> computer,
            object? argument = null)
            : base(options, serviceProvider, argument, false)
        {
            Computer = computer;
            Initialize(options);
        }

        protected override Task<T> ComputeValueAsync(CancellationToken cancellationToken)
            => Computer.Invoke(this, cancellationToken);
    }
}
