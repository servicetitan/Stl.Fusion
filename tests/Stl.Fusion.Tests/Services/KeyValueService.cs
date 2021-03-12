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
        public record SetCommand(string Key, TValue Value) : ICommand<Unit>
        {
            public SetCommand() : this(null!, default!) { }
        }

        public record RemoveCommand(string Key) : ICommand<Unit>
        {
            public RemoveCommand() : this(default(string)!) { }
        }

        [ComputeMethod]
        Task<Option<TValue>> TryGet(string key, CancellationToken cancellationToken = default);
        [ComputeMethod]
        Task<TValue> Get(string key, CancellationToken cancellationToken = default);
        Task Set(string key, TValue value, CancellationToken cancellationToken = default);
        Task Remove(string key, CancellationToken cancellationToken = default);
        [CommandHandler]
        Task SetCmd(SetCommand cmd, CancellationToken cancellationToken = default);
        [CommandHandler]
        Task RemoveCmd(RemoveCommand cmd, CancellationToken cancellationToken = default);
    }

    public class KeyValueService<TValue> : IKeyValueService<TValue>
    {
        private readonly ConcurrentDictionary<string, TValue> _values = new();

        public virtual Task<Option<TValue>> TryGet(string key, CancellationToken cancellationToken = default)
            => Task.FromResult(_values.TryGetValue(key, out var v) ? Option.Some(v) : default);

#pragma warning disable 1998
        public virtual async Task<TValue> Get(string key, CancellationToken cancellationToken = default)
#pragma warning restore 1998
        {
            if (key.EndsWith("error"))
                throw new ApplicationException("Error!");
            return _values.GetValueOrDefault(key)!;
        }

        public virtual Task Set(string key, TValue value, CancellationToken cancellationToken = default)
            => SetCmd(new IKeyValueService<TValue>.SetCommand(key, value), cancellationToken);

        public virtual Task Remove(string key, CancellationToken cancellationToken = default)
            => RemoveCmd(new IKeyValueService<TValue>.RemoveCommand(key), cancellationToken);

        public virtual Task SetCmd(IKeyValueService<TValue>.SetCommand cmd, CancellationToken cancellationToken = default)
        {
            if (Computed.IsInvalidating()) {
                TryGet(cmd.Key, default).AssertCompleted();
                Get(cmd.Key, default).AssertCompleted();
                return Task.CompletedTask;
            }

            _values[cmd.Key] = cmd.Value;
            return Task.CompletedTask;
        }

        public virtual Task RemoveCmd(IKeyValueService<TValue>.RemoveCommand cmd, CancellationToken cancellationToken = default)
        {
            if (Computed.IsInvalidating()) {
                TryGet(cmd.Key, default).AssertCompleted();
                Get(cmd.Key, default).AssertCompleted();
                return Task.CompletedTask;
            }

            _values.TryRemove(cmd.Key, out _);
            return Task.CompletedTask;
        }
    }

    [ComputeService(typeof(IKeyValueService<string>), Scope = ServiceScope.Services)]
    public class StringKeyValueService : KeyValueService<string> { }
}
