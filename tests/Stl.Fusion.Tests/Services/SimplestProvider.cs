using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Fusion.Tests.Services
{
    public interface ISimplestProvider
    {
        // These two properties are here solely for testing purposes
        int GetValueCallCount { get; }
        int GetCharCountCallCount { get; }

        void SetValue(string value);
        [ComputeMethod]
        Task<string> GetValueAsync();
        [ComputeMethod(ErrorAutoInvalidateTime = 0.1)]
        Task<int> GetCharCountAsync();
    }

    [ComputeService(typeof(ISimplestProvider), Lifetime = ServiceLifetime.Scoped)]
    public class SimplestProvider : ISimplestProvider, IHasId<Type>
    {
        private static volatile string _value = "";
        private readonly bool _isCaching;

        public Type Id => GetType();
        public int GetValueCallCount { get; private set; }
        public int GetCharCountCallCount { get; private set; }

        public SimplestProvider()
            => _isCaching = GetType().Name.EndsWith("Proxy");

        public void SetValue(string value)
        {
            Interlocked.Exchange(ref _value, value);
            Invalidate();
        }

        public virtual Task<string> GetValueAsync()
        {
            GetValueCallCount++;
            return Task.FromResult(_value);
        }

        public virtual async Task<int> GetCharCountAsync()
        {
            GetCharCountCallCount++;
            var value = await GetValueAsync().ConfigureAwait(false);
            return value.Length;
        }

        protected virtual void Invalidate()
        {
            if (!_isCaching)
                return;
            Computed.Invalidate(GetValueAsync);
            // No need to invalidate GetCharCountAsync,
            // since it will be invalidated automatically.
        }
    }
}
