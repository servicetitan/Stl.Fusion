using System.Reactive;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl.Fusion
{
    public static partial class ComputedEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T Strip<T>(this IComputed<T>? computed)
            => computed != null ? computed.Value : default!;

        public static Task InvalidatedAsync<T>(this IComputed<T> computed, CancellationToken cancellationToken = default)
        {
            if (computed.State == ComputedState.Invalidated)
                return Task.CompletedTask;
            var ts = TaskSource.New<Unit>(true);
            computed.Invalidated += c => ts.SetResult(default);
            return ts.Task.WithFakeCancellation(cancellationToken);
        }
    }
}
