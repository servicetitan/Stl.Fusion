using System.Linq;
using Autofac;
using Stl.Concurrency;
using Stl.Extensibility;
using Stl.OS;
using Stl.Pooling;
using Stl.Tests.Purifier.Model;

namespace Stl.Tests.Purifier.Services
{
    public interface ITestDbContextPool
    {
        Owned<TestDbContext, Disposable<ResourceLease<ILifetimeScope>>> Rent();
    }

    public class TestDbContextPool : ITestDbContextPool
    {
        private readonly ILifetimeScope _container;
        private ConcurrentPool<ILifetimeScope> _pool;

        public TestDbContextPool(ILifetimeScope container)
        {
            _container = container;
            _pool = new ConcurrentPool<ILifetimeScope>(() => {
                var scope = _container.BeginLifetimeScope();
                scope.Resolve<TestDbContext>();
                return scope;
            }, HardwareInfo.ProcessorCount * 64);
        }

        public Owned<TestDbContext, Disposable<ResourceLease<ILifetimeScope>>> Rent()
        {
            var resourceLease = _pool.Rent();
            var dbContext = resourceLease.Resource.Resolve<TestDbContext>();
            var disposable = Disposable.New(resourceLease, l => {
                var container = l.Resource;
                var dbContext1 = container.Resolve<TestDbContext>();
                if (dbContext1.ChangeTracker.Entries().Any())
                    // Damn, tracked entries -- we can't release it back to pool
                    container.Dispose(); 
                else
                    // Try to release it back to pool or dispose
                    l.Dispose();
            });

            return new Owned<TestDbContext, Disposable<ResourceLease<ILifetimeScope>>>(
                dbContext, disposable);
        }
    }
}
