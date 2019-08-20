using System;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl
{
    public static class Disposable
    {
        public static Disposable<Action> New(Action onDispose)
            => new Disposable<Action>(action => action.Invoke(), onDispose);

        public static Disposable<TState> New<TState>(Action<TState> onDispose, TState state)
            => new Disposable<TState>(onDispose, state);

        public static AsyncDisposable<Func<ValueTask>> New(Func<ValueTask> onDisposeAsync)
            => new AsyncDisposable<Func<ValueTask>>(func => func.Invoke(), onDisposeAsync);

        public static AsyncDisposable<TState> New<TState>(Func<TState, ValueTask> onDisposeAsync, TState state)
            => new AsyncDisposable<TState>(onDisposeAsync, state);
    }

    public readonly struct Disposable<TState> : IDisposable
    {
        private readonly Action<TState> _onDispose;
        private readonly TState _state;

        public Disposable(Action<TState> onDispose, TState state)
        {
            _onDispose = onDispose;
            _state = state;
        }

        public void Dispose()
        {
            _onDispose?.Invoke(_state);
        }
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
