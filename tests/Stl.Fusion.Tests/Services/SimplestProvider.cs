using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Stl.Async;
using Stl.CommandR;
using Stl.CommandR.Configuration;

namespace Stl.Fusion.Tests.Services
{
    public record SetValueCommand : ICommand<Unit>
    {
        public string Value { get; init; } = "";
    }

    public interface ISimplestProvider
    {
        // These two properties are here solely for testing purposes
        int GetValueCallCount { get; }
        int GetCharCountCallCount { get; }

        void SetValue(string value);
        [ComputeMethod(KeepAliveTime = 10)]
        Task<string> GetValue();
        [ComputeMethod(KeepAliveTime = 0.5, ErrorAutoInvalidateTime = 0.5)]
        Task<int> GetCharCount();

        [CommandHandler]
        Task SetValue(SetValueCommand command, CancellationToken cancellationToken = default);
    }

    [ComputeService(typeof(ISimplestProvider), Lifetime = ServiceLifetime.Scoped, Scope = ServiceScope.Services)]
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

        public virtual Task<string> GetValue()
        {
            GetValueCallCount++;
            return Task.FromResult(_value);
        }

        public virtual async Task<int> GetCharCount()
        {
            GetCharCountCallCount++;
            var value = await GetValue().ConfigureAwait(false);
            return value.Length;
        }

        public virtual Task SetValue(SetValueCommand command, CancellationToken cancellationToken = default)
        {
            SetValue(command.Value);
            return Task.CompletedTask;
        }

        protected virtual void Invalidate()
        {
            if (!_isCaching)
                return;

            using (Computed.Invalidate()) {
                GetValue().AssertCompleted();
            }
            // No need to invalidate GetCharCount,
            // since it will be invalidated automatically.
        }
    }
}
