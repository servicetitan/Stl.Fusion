using System;
using System.Reactive.Subjects;
using System.Threading;

namespace Stl.Purifier.Internal
{
    // A copy of Subject<T> allowing to add Dispose handler
    public class SubjectWithDisposer<T, TState> : SubjectBase<T>
    {
        private static readonly SubjectDisposable[] Terminated = new SubjectDisposable[0];
        private static readonly SubjectDisposable[] Disposed = new SubjectDisposable[0];

        private SubjectDisposable[] _observers;
        private Exception? _exception;
        private TState _state;
        private Action<TState> _disposer;

        public override bool HasObservers => Volatile.Read(ref _observers).Length != 0;
        public override bool IsDisposed => Volatile.Read(ref _observers) == Disposed;

        public SubjectWithDisposer(TState state, Action<TState> disposer)
        {
            _state = state;
            _disposer = disposer;
            Volatile.Write(ref _observers, Array.Empty<SubjectDisposable>());
        }

        public override void Dispose()
        {
            var disposed = Disposed != Interlocked.Exchange(ref _observers, Disposed);
            _exception = null;
            if (disposed)
                _disposer?.Invoke(_state);
        }

        public override void OnCompleted()
        {
            for (;;) {
                var observers = Volatile.Read(ref _observers);
                if (observers == Disposed) {
                    _exception = null!;
                    ThrowDisposed();
                    break;
                }
                if (observers == Terminated)
                    break;
                if (Interlocked.CompareExchange(ref _observers, Terminated, observers) == observers) {
                    foreach (var observer in observers)
                        observer.Observer?.OnCompleted();
                    break;
                }
            }
        }

        public override void OnError(Exception error)
        {
            if (error == null)
                throw new ArgumentNullException(nameof(error));

            for (;;) {
                var observers = Volatile.Read(ref _observers);
                if (observers == Disposed) {
                    _exception = null!;
                    ThrowDisposed();
                    break;
                }
                if (observers == Terminated)
                    break;
                _exception = error;
                if (Interlocked.CompareExchange(ref _observers, Terminated, observers) == observers) {
                    foreach (var observer in observers)
                        observer.Observer?.OnError(error);
                    break;
                }
            }
        }

        public override void OnNext(T value)
        {
            var observers = Volatile.Read(ref _observers);
            if (observers == Disposed) {
                _exception = null;
                ThrowDisposed();
                return;
            }
            foreach (var observer in observers)
                observer.Observer?.OnNext(value);
        }

        public override IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null)
                throw new ArgumentNullException(nameof(observer));

            var disposable = default(SubjectDisposable);
            for (;;) {
                var observers = Volatile.Read(ref _observers);
                if (observers == Disposed) {
                    _exception = null;
                    ThrowDisposed();
                    break;
                }
                if (observers == Terminated) {
                    var ex = _exception;
                    if (ex != null)
                        observer.OnError(ex);
                    else
                        observer.OnCompleted();
                    break;
                }
                if (disposable == null)
                    disposable = new SubjectDisposable(this, observer);

                var n = observers.Length;
                var b = new SubjectDisposable[n + 1];
                Array.Copy(observers, 0, b, 0, n);
                b[n] = disposable;
                if (Interlocked.CompareExchange(ref _observers, b, observers) == observers)
                    return disposable;
            }
            return System.Reactive.Disposables.Disposable.Empty;
        }

        // Private members

        private void Unsubscribe(SubjectDisposable observer)
        {
            for (;;) {
                var a = Volatile.Read(ref _observers);
                var n = a.Length;
                if (n == 0)
                    break;

                var j = Array.IndexOf(a, observer);
                if (j < 0)
                    break;

                SubjectDisposable[] b;
                if (n == 1)
                    b = Array.Empty<SubjectDisposable>();
                else {
                    b = new SubjectDisposable[n - 1];
                    Array.Copy(a, 0, b, 0, j);
                    Array.Copy(a, j + 1, b, j, n - j - 1);
                }
                if (Interlocked.CompareExchange(ref _observers, b, a) == a)
                    break;
            }
        }

        private void ThrowDisposed() => throw new ObjectDisposedException(string.Empty);

        private sealed class SubjectDisposable : IDisposable
        {
            private SubjectWithDisposer<T, TState> _subject;
            private IObserver<T>? _observer;

            public IObserver<T>? Observer => Volatile.Read(ref _observer);

            public SubjectDisposable(SubjectWithDisposer<T, TState> subject, IObserver<T> observer)
            {
                _subject = subject;
                Volatile.Write(ref _observer, observer);
            }

            public void Dispose()
            {
                var observer = Interlocked.Exchange(ref _observer, null);
                if (observer == null)
                    return;

                _subject.Unsubscribe(this);
                _subject = null!;
            }
        }
    }
}
