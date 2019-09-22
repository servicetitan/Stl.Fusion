using System;

namespace Stl
{
    public static class Disposable
    {
        public static Disposable<Action> New(Action onDispose)
            => new Disposable<Action>(action => action.Invoke(), onDispose);

        public static Disposable<TState> New<TState>(Action<TState> onDispose, TState state)
            => new Disposable<TState>(onDispose, state);
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
}
