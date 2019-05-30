using System;

namespace Stl
{
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
