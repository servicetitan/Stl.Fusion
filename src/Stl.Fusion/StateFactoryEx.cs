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
            Result<T> initialOutput)
        {
            var options = new MutableState<T>.Options();
            return factory.NewMutable(options, initialOutput);
        }

        public static IMutableState<T> NewMutable<T>(
            this IStateFactory factory,
            Option<Result<T>> initialOutput = default)
        {
            var options = new MutableState<T>.Options();
            return factory.NewMutable(options, initialOutput);
        }

        public static ILiveState<T> NewLive<T>(
            this IStateFactory factory,
            Func<ILiveState<T>, CancellationToken, Task<T>> computer)
        {
            var options = new LiveState<T>.Options();
            return factory.NewLive(options, computer);
        }

        // With builder

        public static IMutableState<T> NewMutable<T>(
            this IStateFactory factory,
            Action<MutableState<T>.Options> optionsBuilder,
            Result<T> initialOutput)
        {
            var options = new MutableState<T>.Options();
            optionsBuilder.Invoke(options);
            return factory.NewMutable(options, initialOutput);
        }

        public static IMutableState<T> NewMutable<T>(
            this IStateFactory factory,
            Action<MutableState<T>.Options> optionsBuilder,
            Option<Result<T>> initialOutput = default)
        {
            var options = new MutableState<T>.Options();
            optionsBuilder.Invoke(options);
            return factory.NewMutable(options, initialOutput);
        }

        public static ILiveState<T> NewLive<T>(
            this IStateFactory factory,
            Action<LiveState<T>.Options> optionsBuilder,
            Func<ILiveState<T>, CancellationToken, Task<T>> computer)
        {
            var options = new LiveState<T>.Options();
            optionsBuilder.Invoke(options);
            return factory.NewLive(options, computer);
        }
    }
}
