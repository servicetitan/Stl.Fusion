using System;

namespace Stl
{
    public static class Disposable
    {
        public static Disposable<Action> New(Action onDispose)
            => new Disposable<Action>(onDispose, action => action.Invoke());

        public static Disposable<TState> New<TState>(TState state, Action<TState> onDispose)
            => new Disposable<TState>(state, onDispose);
    }

    public readonly struct Disposable<TState> : IDisposable
    {
        private readonly TState _state;
        private readonly Action<TState> _onDispose;

        public Disposable(TState state, Action<TState> onDispose)
        {
            _state = state;
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose?.Invoke(_state);
        }
    }
}
