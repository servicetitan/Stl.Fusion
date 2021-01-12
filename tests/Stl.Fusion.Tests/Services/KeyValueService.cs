using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.CommandR;
using Stl.CommandR.Configuration;

namespace Stl.Fusion.Tests.Services
{

    public interface IKeyValueService<TValue>
    {
        public record SetCommand(string Key, TValue Value) : ICommand<Unit> { }
        public record RemoveCommand(string Key) : ICommand<Unit> { }

        [ComputeMethod]
        Task<Option<TValue>> TryGetAsync(string key, CancellationToken cancellationToken = default);
        [ComputeMethod]
        Task<TValue> GetAsync(string key, CancellationToken cancellationToken = default);
        Task SetAsync(string key, TValue value, CancellationToken cancellationToken = default);
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
        [CommandHandler]
        Task SetCommandAsync(SetCommand cmd, CancellationToken cancellationToken = default);
        [CommandHandler]
        Task RemoveCommandAsync(RemoveCommand cmd, CancellationToken cancellationToken = default);
    }

    public class KeyValueService<TValue> : IKeyValueService<TValue>
    {
        private readonly ConcurrentDictionary<string, TValue> _values = new();

        public virtual Task<Option<TValue>> TryGetAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromResult(_values.TryGetValue(key, out var v) ? Option.Some(v) : default);

#pragma warning disable 1998
        public virtual async Task<TValue> GetAsync(string key, CancellationToken cancellationToken = default)
#pragma warning restore 1998
        {
            if (key.EndsWith("error"))
                throw new ApplicationException("Error!");
            return _values.GetValueOrDefault(key)!;
        }

        public virtual Task SetAsync(string key, TValue value, CancellationToken cancellationToken = default)
            => SetCommandAsync(new IKeyValueService<TValue>.SetCommand(key, value), cancellationToken);

        public virtual Task RemoveAsync(string key, CancellationToken cancellationToken = default)
            => RemoveCommandAsync(new IKeyValueService<TValue>.RemoveCommand(key), cancellationToken);

        public virtual Task SetCommandAsync(IKeyValueService<TValue>.SetCommand cmd, CancellationToken cancellationToken = default)
        {
            _values[cmd.Key] = cmd.Value;

            using (Computed.Invalidate()) {
                TryGetAsync(cmd.Key, default).AssertCompleted();
                GetAsync(cmd.Key, default).AssertCompleted();
            };
            return Task.CompletedTask;
        }

        public virtual Task RemoveCommandAsync(IKeyValueService<TValue>.RemoveCommand cmd, CancellationToken cancellationToken = default)
        {
            _values.TryRemove(cmd.Key, out _);

            using (Computed.Invalidate()) {
                TryGetAsync(cmd.Key, default).AssertCompleted();
                GetAsync(cmd.Key, default).AssertCompleted();
            };
            return Task.CompletedTask;
        }
    }

    [ComputeService(typeof(IKeyValueService<string>), Scope = ServiceScope.Services)]
    public class StringKeyValueService : KeyValueService<string> { }
}
