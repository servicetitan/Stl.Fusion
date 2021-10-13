using System;
using System.Threading;
using Microsoft.AspNetCore.Components;
using Stl.Internal;

namespace Stl.Fusion.Blazor
{
    public class BlazorCircuitContext : IDisposable
    {
        private volatile int _isDisposing;
        private volatile int _isPrerendering;
        private ComponentBase? _rootComponent;

        public bool IsPrerendering => _isPrerendering != 0;
        public bool IsDisposing => _isDisposing != 0;

        public ComponentBase RootComponent {
            get => _rootComponent ?? throw Errors.NotInitialized(nameof(RootComponent));
            set {
                if (_rootComponent == value)
                    return;
                if (_rootComponent != null)
                    throw Errors.AlreadyInitialized(nameof(RootComponent));
                _rootComponent = value;
            }
        }

        public Dispatcher Dispatcher
            => RootComponent.GetDispatcher();

        public ClosedDisposable<(BlazorCircuitContext, int)> Prerendering(bool isPrerendering = true)
        {
            var oldIsPrerendering = Interlocked.Exchange(ref _isPrerendering, isPrerendering ? 1 : 0);
            return new ClosedDisposable<(BlazorCircuitContext Context, int OldIsPrerendering)>(
                (this, oldIsPrerendering),
                state => Interlocked.Exchange(ref state.Context._isPrerendering, state.OldIsPrerendering));
        }

        public void Dispose()
        {
            if (0 != Interlocked.CompareExchange(ref _isDisposing, 1, 0))
                return;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        { }
    }
}
