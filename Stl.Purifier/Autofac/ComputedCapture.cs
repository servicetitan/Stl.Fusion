using System;
using System.Threading;

namespace Stl.Purifier.Autofac
{
    public class ComputedCapture : IDisposable
    {
        private static readonly AsyncLocal<ComputedCapture?> CurrentLocal = new AsyncLocal<ComputedCapture?>();
        
        public static ComputedCapture? Current => CurrentLocal.Value;

        private bool _isDisposed;
        private ComputedCapture? _previous;
        private volatile IComputed? _captured;

        public IComputed? Captured {
            get => _captured;
            private set => Interlocked.Exchange(ref _captured, value);
        }

        public static ComputedCapture<T> New<T>() => new ComputedCapture<T>();
        public static ComputedCapture New() => new ComputedCapture();
        
        protected ComputedCapture()
        {
            _previous = CurrentLocal.Value;
            CurrentLocal.Value = this;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
            CurrentLocal.Value = _previous;
            _previous = null;
        }

        public static bool TryCapture(IComputed captured)
        {
            var current = Current;
            if (current == null)
                return false;
            var previous = Interlocked.CompareExchange(ref current._captured, captured, null);
            return previous == null;
        }
    }

    public class ComputedCapture<T> : ComputedCapture
    {
        public new IComputed<T>? Captured => base.Captured as IComputed<T>;

        internal ComputedCapture() : base() { }
    }
}
