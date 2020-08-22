using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion
{
    public abstract class ComputedUpdater<T>
    {
        public static ComputedUpdater<T> None = new NoComputedUpdater<T>();

        public abstract Task Update(IComputed<T> prevComputed, IComputed<T> nextComputed, CancellationToken cancellationToken);

        // Operators

        public static implicit operator ComputedUpdater<T>(
            Func<IComputed<T>, IComputed<T>, CancellationToken, Task> updater)
            => new DefaultComputedUpdater<T>(updater);
        public static implicit operator ComputedUpdater<T>(
            Func<IComputed<T>, CancellationToken, Task<T>> updater)
            => new CastingComputedUpdater1<T>(updater);
        public static implicit operator ComputedUpdater<T>(
            Func<CancellationToken, Task<T>> updater)
            => new CastingComputedUpdater0<T>(updater);
    }

    internal class NoComputedUpdater<T> : ComputedUpdater<T>
    {
        public NoComputedUpdater() {}

        public override Task Update(IComputed<T> prevComputed, IComputed<T> nextComputed, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    internal class DefaultComputedUpdater<T> : ComputedUpdater<T>
    {
        public Func<IComputed<T>, IComputed<T>, CancellationToken, Task> Updater { get; }

        public DefaultComputedUpdater(Func<IComputed<T>, IComputed<T>, CancellationToken, Task> updater)
            => Updater = updater ?? throw new ArgumentOutOfRangeException(nameof(updater));

        public override Task Update(IComputed<T> prevComputed, IComputed<T> nextComputed, CancellationToken cancellationToken)
            => Updater.Invoke(prevComputed, nextComputed, cancellationToken);
    }

    internal class CastingComputedUpdater1<T> : ComputedUpdater<T>
    {
        public Func<IComputed<T>, CancellationToken, Task<T>> Updater { get; }

        public CastingComputedUpdater1(Func<IComputed<T>, CancellationToken, Task<T>> updater)
            => Updater = updater ?? throw new ArgumentOutOfRangeException(nameof(updater));

        public override async Task Update(IComputed<T> prevComputed, IComputed<T> nextComputed, CancellationToken cancellationToken)
        {
            try {
                var value = await Updater.Invoke(prevComputed, cancellationToken).ConfigureAwait(false);
                nextComputed.TrySetOutput(Result.New(value));
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                nextComputed.TrySetOutput(Result.Error<T>(e));
            }
        }
    }

    internal class CastingComputedUpdater0<T> : ComputedUpdater<T>
    {
        public Func<CancellationToken, Task<T>> Updater { get; }

        public CastingComputedUpdater0(Func<CancellationToken, Task<T>> updater)
            => Updater = updater ?? throw new ArgumentOutOfRangeException(nameof(updater));

        public override async Task Update(IComputed<T> prevComputed, IComputed<T> nextComputed, CancellationToken cancellationToken)
        {
            try {
                var value = await Updater.Invoke(cancellationToken).ConfigureAwait(false);
                nextComputed.TrySetOutput(Result.New(value));
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                nextComputed.TrySetOutput(Result.Error<T>(e));
            }
        }
    }
}
