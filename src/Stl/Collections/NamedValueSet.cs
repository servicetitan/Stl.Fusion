using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Newtonsoft.Json;
using Stl.Reflection;
using Stl.Text;

namespace Stl.Collections
{
    public class NamedValueSet : IServiceProvider
    {
        private volatile ImmutableDictionary<Symbol, object> _items;

        public ImmutableDictionary<Symbol, object> Items => _items;

        public object? this[Symbol key] {
            get => _items.TryGetValue(key, out var v) ? v : null;
            set {
                var spinWait = new SpinWait();
                var properties = _items;
                for (;;) {
                    var newProperties = value != null
                        ? properties.SetItem(key, value)
                        : properties.Remove(key);
                    var oldProperties = Interlocked.CompareExchange(ref _items, newProperties, properties);
                    if (oldProperties == properties)
                        return;
                    properties = oldProperties;
                    spinWait.SpinOnce();
                }
            }
        }

        public object? this[Type type] {
            get => this[type.ToSymbol()];
            set => this[type.ToSymbol()] = value;
        }

        public NamedValueSet()
            => _items = ImmutableDictionary<Symbol, object>.Empty;
        [JsonConstructor]
        public NamedValueSet(ImmutableDictionary<Symbol, object>? items)
            => _items = items ?? ImmutableDictionary<Symbol, object>.Empty;

        public object? GetService(Type serviceType)
            => this[serviceType];

        public T Get<T>()
        {
            var value = this[typeof(T)];
            if (value == null)
                throw new KeyNotFoundException();
            return (T) value;
        }

        public Option<T> TryGet<T>()
        {
            var value = this[typeof(T)];
            if (value == null)
                return Option<T>.None;
            return Option<T>.Some((T) value);
        }

        // ReSharper disable once HeapView.PossibleBoxingAllocation
        public void Set<T>(T value) => this[typeof(T)] = value;
        public void Remove<T>() => this[typeof(T)] = null;
    }
}
