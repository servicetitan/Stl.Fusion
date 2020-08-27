using System;

namespace Stl.Fusion
{
    public interface IComputedState : IState { }
    public interface IComputedState<T> : IState<T>, IComputedState { }

    public abstract class ComputedState<T> : State<T>, IComputedState<T>
    {
        public new class Options : State<T>.Options { }

        public ComputedState(
            Options options, IServiceProvider serviceProvider,
            object? argument = null, bool initialize = true)
            : base(options, serviceProvider, argument, false)
        {
            if (initialize) Initialize(options);
        }
    }
}
