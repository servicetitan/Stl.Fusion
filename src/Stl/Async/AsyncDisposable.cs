using System;
using System.Threading.Tasks;

namespace Stl.Async
{
    public static class AsyncDisposable
    {
        public static AsyncDisposable<Func<ValueTask>> New(Func<ValueTask> onDisposeAsync)
            => new AsyncDisposable<Func<ValueTask>>(func => func.Invoke(), onDisposeAsync);

        public static AsyncDisposable<TState> New<TState>(Func<TState, ValueTask> onDisposeAsync, TState state)
            => new AsyncDisposable<TState>(onDisposeAsync, state);
    }

    public readonly struct AsyncDisposable<TState> : IAsyncDisposable
    {
        private readonly Func<TState, ValueTask> _onDisposeAsync;
        private readonly TState _state;

        public AsyncDisposable(Func<TState, ValueTask> onDisposeAsync, TState state)
        {
            _onDisposeAsync = onDisposeAsync;
            _state = state;
        }

        public ValueTask DisposeAsync()
            => _onDisposeAsync?.Invoke(_state) ?? ValueTaskEx.CompletedTask;
    }
}
