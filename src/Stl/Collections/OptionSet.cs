using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using Stl.Reflection;
using Stl.Serialization;
using Stl.Text;

namespace Stl.Collections
{
    public class OptionSet : IServiceProvider
    {
        private volatile ImmutableDictionary<Symbol, object> _items;

        [JsonIgnore]
        public ImmutableDictionary<Symbol, object> Items {
            get => _items;
            set => _items = value;
        }

        [JsonPropertyName(nameof(Items)),  Newtonsoft.Json.JsonIgnore]
        public Dictionary<string, NewtonsoftJsonSerialized<object>> JsonCompatibleItems
            => Items.ToDictionary(
                p => p.Key.Value,
                p => NewtonsoftJsonSerialized.New(p.Value));

        public object? this[Symbol key] {
            get => _items.TryGetValue(key, out var v) ? v : null;
            set {
                var spinWait = new SpinWait();
                var items = _items;
                while (true) {
                    var newItems = value != null
                        ? items.SetItem(key, value)
                        : items.Remove(key);
                    var oldItems = Interlocked.CompareExchange(ref _items, newItems, items);
                    if (oldItems == items)
                        return;
                    items = oldItems;
                    spinWait.SpinOnce();
                }
            }
        }

        public object? this[Type type] {
            get => this[type.ToSymbol()];
            set => this[type.ToSymbol()] = value;
        }

        public OptionSet()
            => _items = ImmutableDictionary<Symbol, object>.Empty;

        [Newtonsoft.Json.JsonConstructor]
        public OptionSet(ImmutableDictionary<Symbol, object>? items)
            => _items = items ?? ImmutableDictionary<Symbol, object>.Empty;

        [JsonConstructor]
        public OptionSet(Dictionary<string, NewtonsoftJsonSerialized<object>>? jsonCompatibleItems)
            : this(jsonCompatibleItems?.ToImmutableDictionary(p => (Symbol) p.Key, p => p.Value.Value))
        { }

        public object? GetService(Type serviceType)
            => this[serviceType];

        public T? TryGet<T>()
            => (T?) this[typeof(T)]!;

        public bool TryGet<T>(out T value)
        {
            var objValue = this[typeof(T)];
            if (objValue == null) {
                value = default!;
                return false;
            }
            value = (T) objValue;
            return true;
        }

        public T Get<T>()
        {
            var value = this[typeof(T)];
            if (value == null)
                throw new KeyNotFoundException();
            return (T) value;
        }

        public T GetOrDefault<T>(T @default)
        {
            var value = this[typeof(T)];
            return value == null ? @default : (T) value;
        }

        // ReSharper disable once HeapView.PossibleBoxingAllocation
        public void Set<T>(T value) => this[typeof(T)] = value;
        public void Remove<T>() => this[typeof(T)] = null;

        public void Clear()
        {
            var spinWait = new SpinWait();
            var items = _items;
            while (true) {
                var oldItems = Interlocked.CompareExchange(
                    ref _items, ImmutableDictionary<Symbol, object>.Empty, items);
                if (oldItems == items || oldItems.Count == 0)
                    return;
                items = oldItems;
                spinWait.SpinOnce();
            }
        }
    }
}
