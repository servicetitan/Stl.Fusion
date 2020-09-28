using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion
{
    public static class StateFactoryEx
    {
        // With default options

        public static IMutableState<T> NewMutable<T>(
            this IStateFactory factory,
            Result<T> initialOutput,
            object? argument = null)
        {
            var options = new MutableState<T>.Options();
            return factory.NewMutable(options, initialOutput, argument);
        }

        public static IMutableState<T> NewMutable<T>(
            this IStateFactory factory,
            Option<Result<T>> initialOutput = default,
            object? argument = null)
        {
            var options = new MutableState<T>.Options();
            return factory.NewMutable(options, initialOutput, argument);
        }

        public static IComputedState<T> NewComputed<T>(
            this IStateFactory factory,
            Func<IComputedState<T>, CancellationToken, Task<T>> computer,
            object? argument = null)
        {
            var options = new ComputedState<T>.Options();
            return factory.NewComputed(options, computer, argument);
        }

        public static ILiveState<T> NewLive<T>(
            this IStateFactory factory,
            Func<ILiveState<T>, CancellationToken, Task<T>> computer,
            object? argument = null)
        {
            var options = new LiveState<T>.Options();
            return factory.NewLive(options, computer, argument);
        }

        public static ILiveState<T, TLocals> NewLive<T, TLocals>(
            this IStateFactory factory,
            Func<ILiveState<T, TLocals>, CancellationToken, Task<T>> computer,
            object? argument = null)
        {
            var options = new LiveState<T, TLocals>.Options();
            return factory.NewLive(options, computer, argument);
        }

        // With builder

        public static IMutableState<T> NewMutable<T>(
            this IStateFactory factory,
            Action<MutableState<T>.Options> optionsBuilder,
            Result<T> initialOutput,
            object? argument = null)
        {
            var options = new MutableState<T>.Options();
            optionsBuilder.Invoke(options);
            return factory.NewMutable(options, initialOutput, argument);
        }

        public static IMutableState<T> NewMutable<T>(
            this IStateFactory factory,
            Action<MutableState<T>.Options> optionsBuilder,
            Option<Result<T>> initialOutput = default,
            object? argument = null)
        {
            var options = new MutableState<T>.Options();
            optionsBuilder.Invoke(options);
            return factory.NewMutable(options, initialOutput, argument);
        }

        public static IComputedState<T> NewComputed<T>(
            this IStateFactory factory,
            Action<ComputedState<T>.Options> optionsBuilder,
            Func<IComputedState<T>, CancellationToken, Task<T>> computer,
            object? argument = null)
        {
            var options = new ComputedState<T>.Options();
            optionsBuilder.Invoke(options);
            return factory.NewComputed(options, computer, argument);
        }

        public static ILiveState<T> NewLive<T>(
            this IStateFactory factory,
            Action<LiveState<T>.Options> optionsBuilder,
            Func<ILiveState<T>, CancellationToken, Task<T>> computer,
            object? argument = null)
        {
            var options = new LiveState<T>.Options();
            optionsBuilder.Invoke(options);
            return factory.NewLive(options, computer, argument);
        }

        public static ILiveState<T, TLocals> NewLive<T, TLocals>(
            this IStateFactory factory,
            Action<LiveState<T, TLocals>.Options> optionsBuilder,
            Func<ILiveState<T, TLocals>, CancellationToken, Task<T>> computer,
            object? argument = null)
        {
            var options = new LiveState<T, TLocals>.Options();
            optionsBuilder.Invoke(options);
            return factory.NewLive(options, computer, argument);
        }
    }
}
