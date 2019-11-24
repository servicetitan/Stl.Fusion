using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Stl.Collections;
using Stl.Extensibility;
using Stl.ImmutableModel.Internal;
using Stl.Reflection;
using Stl.Text;

namespace Stl.ImmutableModel.Reflection
{
    public class NodeTypeDef
    {
        private static readonly ConcurrentDictionary<Type, NodeTypeDef> _cache =
            new ConcurrentDictionary<Type, NodeTypeDef>();

        public Type Type { get; }
        public bool IsCollectionNode { get; }
        public IReadOnlyDictionary<Symbol, INodePropertyDef> Properties { get; }
        public IReadOnlyDictionary<Symbol, INodePropertyDef> NodeProperties { get; }
        public IReadOnlyDictionary<Symbol, INodePropertyDef> CollectionNodeProperties { get; }
        public IReadOnlyDictionary<Symbol, INodePropertyDef> FreezableProperties { get; }

        public static NodeTypeDef Get(Type type)
        {
            if (_cache.TryGetValue(type, out var r))
                return r;
            lock (_cache) {
                if (_cache.TryGetValue(type, out r))
                    return r;
                var bases = EnumerableEx.One(type).Concat(type.GetAllBaseTypes());
                var bindingFlags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                var methodInfo = bases
                    .Select(t => t.GetMethod(nameof(Node.CreateNodeTypeDef), bindingFlags))
                    .FirstOrDefault(mi => mi != null);
                if (methodInfo == null)
                    throw Errors.CannotCreateNodeTypeDef(type);
                r = (NodeTypeDef) methodInfo.Invoke(null, new object?[] {type})!;
                _cache[type] = r;
                return r;
            }
        }

        public NodeTypeDef(Type type)
        {
            Type = type;
            IsCollectionNode = typeof(CollectionNodeBase).IsAssignableFrom(type);
            
            var properties = new Dictionary<Symbol, INodePropertyDef>();
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
            foreach (var property in type.GetProperties(bindingFlags)) {
                // Must not be 'Key' property
                if (property.Name == nameof(INode.Key))
                    continue;
                // Must not be indexer
                if (property.GetIndexParameters().Length != 0)
                    continue;
                // Must have both getter & setter
                if (property.GetGetMethod(false) == null || property.GetSetMethod(false) == null)
                    continue;
                // Must not have NodePropertyAttribute.IsNodeProperty == false
                var npa = property.GetCustomAttribute<NodePropertyAttribute>(true) 
                    ?? new NodePropertyAttribute();
                if (!npa.IsNodeProperty)
                    continue;
                var propertyName = (Symbol) property.Name;
                var propertyInfoType = typeof(NodePropertyDef<>).MakeGenericType(property.PropertyType);
                var propertyInfo = (INodePropertyDef) Activator.CreateInstance(propertyInfoType, type, propertyName)!;
                properties.Add(propertyName, propertyInfo);
            }

            Properties = new ReadOnlyDictionary<Symbol, INodePropertyDef>(properties);
            NodeProperties = new ReadOnlyDictionary<Symbol, INodePropertyDef>(
                properties.Where(p => p.Value.IsNode).ToDictionary());
            CollectionNodeProperties = new ReadOnlyDictionary<Symbol, INodePropertyDef>(
                properties.Where(p => p.Value.IsCollectionNode).ToDictionary());
            FreezableProperties = new ReadOnlyDictionary<Symbol, INodePropertyDef>(
                properties.Where(p => p.Value.IsFreezable).ToDictionary());
        }

        public virtual IEnumerable<KeyValuePair<ItemKey, object?>> GetAllItems(INode node)
        {
            foreach (var (name, propertyDef) in Properties) {
                var getter = (Func<INode, object>?) propertyDef.UntypedGetter;
                if (getter == null)
                    continue;
                var value = getter.Invoke(node);
                yield return KeyValuePair.Create((ItemKey) name, value)!;
            }

            var hasOptions = (IHasOptions) node;
            foreach (var (key, option) in hasOptions.GetAllOptions())
                yield return KeyValuePair.Create((ItemKey) key, option)!;
        }

        public virtual void GetFreezableItems(INode node, ref ListBuffer<KeyValuePair<ItemKey, IFreezable>> output)
        {
            foreach (var (name, propertyDef) in FreezableProperties) {
                var getter = (Func<INode, object>?) propertyDef.UntypedGetter;
                if (getter == null)
                    continue;
                var value = getter.Invoke(node);
                if (value is IFreezable f)
                    output.Add(KeyValuePair.Create((ItemKey) name, f));
            }

            var hasOptions = (IHasOptions) node;
            foreach (var (key, option) in hasOptions.GetAllOptions()) {
                if (option is IFreezable f)
                    output.Add(KeyValuePair.Create((ItemKey) key, f));
            }
        }

        public virtual void GetNodeItems(INode node, ref ListBuffer<KeyValuePair<ItemKey, INode>> output)
        {
            foreach (var (name, propertyDef) in NodeProperties) {
                var getter = (Func<INode, object>?) propertyDef.UntypedGetter;
                if (getter == null)
                    continue;
                var value = getter.Invoke(node);
                if (value is INode n)
                    output.Add(KeyValuePair.Create((ItemKey) name, n));
            }

            var hasOptions = (IHasOptions) node;
            foreach (var (key, option) in hasOptions.GetAllOptions()) {
                if (option is INode n)
                    output.Add(KeyValuePair.Create((ItemKey) key, n));
            }
        }

        public virtual void GetCollectionNodeItems(INode node, ref ListBuffer<KeyValuePair<ItemKey, ICollectionNode>> output)
        {
            foreach (var (name, propertyDef) in CollectionNodeProperties) {
                var getter = (Func<INode, object>?) propertyDef.UntypedGetter;
                if (getter == null)
                    continue;
                var value = getter.Invoke(node);
                if (value is ICollectionNode n)
                    output.Add(KeyValuePair.Create((ItemKey) name, n));
            }

            var hasOptions = (IHasOptions) node;
            foreach (var (key, option) in hasOptions.GetAllOptions()) {
                if (option is ICollectionNode n)
                    output.Add(KeyValuePair.Create((ItemKey) key, n));
            }
        }

        public virtual bool TryGetItem<T>(INode node, ItemKey itemKey, out T value)
        {
            value = default!;
            var symbol = itemKey.AsSymbol(); 
            if (symbol.IsValidOptionsKey()) {
                if (!node.HasOption(symbol))
                    return false;
                value = node.GetOption<T>(symbol);
                return true;
            }
            var getter = (Func<INode, T>?) Properties[symbol].Getter!;
            if (getter == null)
                return false;
            value = getter.Invoke(node);
            return true;
        }

        public virtual bool TryGetItem(INode node, ItemKey itemKey, out object? value)
        {
            value = null;
            var symbol = itemKey.AsSymbol(); 
            if (symbol.IsValidOptionsKey()) {
                if (!node.HasOption(symbol))
                    return false;
                value = node.GetOption(symbol);
                return true;
            }
            var getter = (Func<INode, object?>?) Properties[symbol].UntypedGetter!;
            if (getter == null)
                return false;
            value = getter.Invoke(node);
            return true;
        }

        public virtual T GetItem<T>(INode node, ItemKey itemKey)
        {
            var symbol = itemKey.AsSymbol(); 
            if (symbol.IsValidOptionsKey())
                return node.GetOption<T>(symbol);
            var getter = (Func<INode, T>) Properties[symbol].Getter!;
            return getter.Invoke(node);
        }

        public virtual object? GetItem(INode node, ItemKey itemKey)
        {
            var symbol = itemKey.AsSymbol(); 
            if (symbol.IsValidOptionsKey())
                return node.GetOption(symbol);
            var getter = (Func<INode, object?>) Properties[symbol].UntypedGetter!;
            return getter.Invoke(node);
        }

        public virtual void SetItem<T>(INode node, ItemKey itemKey, T value)
        {
            var symbol = itemKey.AsSymbol(); 
            if (symbol.IsValidOptionsKey()) {
                node.SetOption(symbol, value);
                return;
            }
            var setter = (Action<INode, T>) Properties[symbol].Setter!;
            setter.Invoke(node, value);
        }

        public virtual void SetItem(INode node, ItemKey itemKey, object? value)
        {
            var symbol = itemKey.AsSymbol(); 
            if (symbol.IsValidOptionsKey()) {
                node.SetOption(symbol, value);
                return;
            }
            var setter = (Action<INode, object?>) Properties[symbol].UntypedSetter!;
            setter.Invoke(node, value);
        }

        public virtual void RemoveItem(INode node, ItemKey itemKey)
        {
            var symbol = itemKey.AsSymbol(); 
            symbol.ThrowIfInvalidOptionsKey();
            node.SetOption(symbol, null);
        }

        // Useful helpers

        public void GetClosestChildNodesByType<TChild>(INode node, ref ListBuffer<TChild> output, bool includeSelf = false)
            where TChild : class, INode
        {
            if (node is TChild n && includeSelf) {
                output.Add(n);
                return;
            }

            var buffer = ListBuffer<KeyValuePair<ItemKey, INode>>.Lease();
            try {
                node.GetDefinition().GetNodeItems(node, ref buffer);
                foreach (var (key, value) in buffer)
                    GetClosestChildNodesByType(value, ref output, true);
            }
            finally {
                buffer.Release();
            }
        }
    }
}
