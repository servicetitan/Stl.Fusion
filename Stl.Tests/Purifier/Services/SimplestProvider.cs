using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Purifier;

namespace Stl.Tests.Purifier.Services
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

    public class SimplestProvider : ISimplestProvider
    {
        private volatile string _value = "";
        private readonly bool _isCaching;

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
