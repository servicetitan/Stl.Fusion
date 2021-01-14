using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Newtonsoft.Json;
using Stl.Reflection;
using Stl.Text;

namespace Stl.Collections
{
    public readonly struct ImmutableOptionSet : IServiceProvider
    {
        public static readonly ImmutableOptionSet Empty = new(ImmutableDictionary<Symbol, object>.Empty);

        private readonly ImmutableDictionary<Symbol, object>? _items;

        public ImmutableDictionary<Symbol, object> Items => _items ?? ImmutableDictionary<Symbol, object>.Empty;

        public object? this[Symbol key] => Items.TryGetValue(key, out var v) ? v : null;
        public object? this[Type type] => this[type.ToSymbol()];

        [JsonConstructor]
        public ImmutableOptionSet(ImmutableDictionary<Symbol, object>? items)
            => _items = items ?? ImmutableDictionary<Symbol, object>.Empty;

        public object? GetService(Type serviceType)
            => this[serviceType];

        public T? TryGet<T>()
            => (T?) this[typeof(T)]!;

        public T Get<T>()
        {
            var value = this[typeof(T)];
            if (value == null)
                throw new KeyNotFoundException();
            return (T) value;
        }

        public ImmutableOptionSet Set(Symbol key, object? value)
            => new(value != null ? Items.SetItem(key, value) : Items.Remove(key));
        public ImmutableOptionSet Set(Type type, object? value)
            => Set(type.ToSymbol(), value);
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        public ImmutableOptionSet Set<T>(T value) => Set(typeof(T), value);
        public ImmutableOptionSet Remove<T>() => Set(typeof(T), null);
    }
}
