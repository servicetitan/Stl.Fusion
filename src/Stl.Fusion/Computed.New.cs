using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.DependencyInjection;
using Stl.Generators;

namespace Stl.Fusion
{
    public static partial class Computed
    {
        // Overloads with ComputedUpdater<T>

        public static IComputed<T> New<T>(
            ComputedUpdater<T> updater,
            Result<T> output = default, bool isConsistent = false)
            => New(null, ComputedOptions.Default, updater, output, isConsistent);

        public static IComputed<T> New<T>(
            ComputedOptions options,
            ComputedUpdater<T> updater,
            Result<T> output = default, bool isConsistent = false)
            => New(null, options, updater, output, isConsistent);

        public static IComputed<T> New<T>(
            IServiceProvider? serviceProvider,
            ComputedUpdater<T> updater,
            Result<T> output = default, bool isConsistent = false)
            => New(serviceProvider, ComputedOptions.Default, updater, output, isConsistent);

        public static IComputed<T> New<T>(
            IServiceProvider? serviceProvider,
            ComputedOptions options,
            ComputedUpdater<T> updater,
            Result<T> output = default, bool isConsistent = false)
        {
            serviceProvider ??= ServiceProviderEx.Empty;
            var input = new StandaloneComputedInput<T>(serviceProvider, updater);
            var version = ConcurrentLTagGenerator.Default.Next();
            input.Computed = new StandaloneComputed<T>(options, input, output, version, isConsistent);
            return input.Computed;
        }

        // Overloads with Func<IComputed<T>, CancellationToken, Task<T>>

        public static IComputed<T> New<T>(
            Func<IComputed<T>, CancellationToken, Task<T>> updater,
            Result<T> output = default, bool isConsistent = false)
            => New(null, ComputedOptions.Default, updater, output, isConsistent);

        public static IComputed<T> New<T>(
            ComputedOptions options,
            Func<IComputed<T>, CancellationToken, Task<T>> updater,
            Result<T> output = default, bool isConsistent = false)
            => New(null, options, updater, output, isConsistent);

        public static IComputed<T> New<T>(
            IServiceProvider? serviceProvider,
            Func<IComputed<T>, CancellationToken, Task<T>> updater,
            Result<T> output = default, bool isConsistent = false)
            => New(serviceProvider, ComputedOptions.Default, updater, output, isConsistent);

        public static IComputed<T> New<T>(
            IServiceProvider? serviceProvider,
            ComputedOptions options,
            Func<IComputed<T>, CancellationToken, Task<T>> updater,
            Result<T> output = default, bool isConsistent = false)
        {
            serviceProvider ??= ServiceProviderEx.Empty;
            var input = new StandaloneComputedInput<T>(serviceProvider, updater);
            var version = ConcurrentLTagGenerator.Default.Next();
            input.Computed = new StandaloneComputed<T>(options, input, output, version, isConsistent);
            return input.Computed;
        }

        // Overloads with Func<CancellationToken, Task<T>>

        public static IComputed<T> New<T>(
            Func<CancellationToken, Task<T>> updater,
            Result<T> output = default, bool isConsistent = false)
            => New(null, ComputedOptions.Default, updater, output, isConsistent);

        public static IComputed<T> New<T>(
            ComputedOptions options,
            Func<CancellationToken, Task<T>> updater,
            Result<T> output = default, bool isConsistent = false)
            => New(null, options, updater, output, isConsistent);

        public static IComputed<T> New<T>(
            IServiceProvider? serviceProvider,
            Func<CancellationToken, Task<T>> updater,
            Result<T> output = default, bool isConsistent = false)
            => New(serviceProvider, ComputedOptions.Default, updater, output, isConsistent);

        public static IComputed<T> New<T>(
            IServiceProvider? serviceProvider,
            ComputedOptions options,
            Func<CancellationToken, Task<T>> updater,
            Result<T> output = default, bool isConsistent = false)
        {
            serviceProvider ??= ServiceProviderEx.Empty;
            var input = new StandaloneComputedInput<T>(serviceProvider, updater);
            var version = ConcurrentLTagGenerator.Default.Next();
            input.Computed = new StandaloneComputed<T>(options, input, output, version, isConsistent);
            return input.Computed;
        }
    }
}
