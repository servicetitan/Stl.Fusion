using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.Tests.Services
{
    public interface ISimplestProvider
    {
        // These two properties are here solely for testing purposes
        int GetValueCallCount { get; }
        int GetCharCountCallCount { get; }

        void SetValue(string value);
        Task<string> GetValueAsync();
        Task<int> GetCharCountAsync();
    }

    [ComputedService(typeof(ISimplestProvider))]
    public class SimplestProvider : ISimplestProvider, IScopedComputedService, IHasId<Type>
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

        [ComputedServiceMethod]
        public virtual Task<string> GetValueAsync()
        {
            GetValueCallCount++;
            return Task.FromResult(_value);
        }

        [ComputedServiceMethod(ErrorAutoInvalidateTime = 0.1)]
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
