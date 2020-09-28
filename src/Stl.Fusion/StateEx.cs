using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion
{
    public static class StateEx
    {
        public static void Invalidate(this IState state, bool andUpdate = false)
        {
            var snapshot = state.Snapshot;
            var computed = snapshot.Computed;
            computed.Invalidate();
            if (!andUpdate)
                return;

            computed.UpdateAsync(false).Ignore();
            if (state is ILiveState liveState) {
                // CancelDelays call may also start the update, but since
                // all computed instances cache their results, triggering
                // the update twice is absolutely fine: it's ~ a tiny cost
                // operation assuming the dependencies are still consistent.
                liveState.UpdateDelayer.CancelDelays();
            }
        }

        public static ValueTask<T> UseAsync<T>(this IState<T> state, CancellationToken cancellationToken = default)
            => state.Computed.UseAsync(cancellationToken);

        public static async ValueTask<TState> UpdateAsync<TState>(
            this TState state, bool addDependency, CancellationToken cancellationToken = default)
            where TState : class, IState
        {
            await state.Computed.UpdateAsync(addDependency, cancellationToken).ConfigureAwait(false);
            return state;
        }
    }
}
