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
        
        public IComputed? Captured { get; private set; }

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
            if (current == null || current.Captured != null)
                return false;
            current.Captured = captured;
            return true;
        }
    }

    public class ComputedCapture<T> : ComputedCapture
    {
        public new IComputed<T>? Captured => base.Captured as IComputed<T>;

        internal ComputedCapture() : base() { }
    }
}
