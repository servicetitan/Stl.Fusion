using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.Concurrency;
using Stl.Extensibility;

namespace Stl.Pooling
{
    public interface IScopedServicePool<TService>
    {
        Owned<TService, IDisposable> Rent();
    }

    public class ScopedServicePool<TService> : IScopedServicePool<TService>
    {
        private readonly ConcurrentPool<IServiceScope> _scopePool;
        private readonly Func<TService, bool> _canReuseHandler;

        public ScopedServicePool(IServiceProvider services,
            Func<TService, bool> canReuseHandler,
            int capacity)
        {
            IServiceScope PoolItemFactory() {
                var scope = services.CreateScope();
                var _ = scope.ServiceProvider.GetRequiredService<TService>();
                return scope;
            }

            _canReuseHandler = canReuseHandler;
            _scopePool = new ConcurrentPool<IServiceScope>(PoolItemFactory, capacity);
        }

        public Owned<TService, IDisposable> Rent()
        {
            var lease = _scopePool.Rent();
            var service = lease.Resource.ServiceProvider.GetRequiredService<TService>();
            var disposable = Disposable.New((this, lease), state => {
                var (self, lease1) = state;
                var scope = lease1.Resource;
                var service1 = scope.ServiceProvider.GetRequiredService<TService>();
                if (self._canReuseHandler.Invoke(service1))
                    lease1.Dispose(); // Return scope back to the pool
                else
                    scope.Dispose(); // Dispose the scope
            });
            return new Owned<TService, IDisposable>(service, disposable);
        }
    }
}
