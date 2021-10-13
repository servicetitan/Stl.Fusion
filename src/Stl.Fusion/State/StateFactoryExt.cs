using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion
{
    public static class StateFactoryExt
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

        public static IComputedState<T> NewComputed<T>(
            this IStateFactory factory,
            Func<IComputedState<T>, CancellationToken, Task<T>> computer)
        {
            var options = new ComputedState<T>.Options();
            return factory.NewComputed(options, computer);
        }

        // With update delayer

        public static IComputedState<T> NewComputed<T>(
            this IStateFactory factory,
            IUpdateDelayer updateDelayer,
            Func<IComputedState<T>, CancellationToken, Task<T>> computer)
        {
            var options = new ComputedState<T>.Options() {
                UpdateDelayer = updateDelayer,
            };
            return factory.NewComputed(options, computer);
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

        public static IComputedState<T> NewComputed<T>(
            this IStateFactory factory,
            Action<ComputedState<T>.Options> optionsBuilder,
            Func<IComputedState<T>, CancellationToken, Task<T>> computer)
        {
            var options = new ComputedState<T>.Options();
            optionsBuilder.Invoke(options);
            return factory.NewComputed(options, computer);
        }
    }
}
