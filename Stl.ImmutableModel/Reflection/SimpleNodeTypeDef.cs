using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Stl.Collections;
using Stl.Extensibility;
using Stl.Internal;
using Stl.Text;

namespace Stl.ImmutableModel.Reflection
{
    public class SimpleNodeTypeDef : NodeTypeDef
    {
        public IReadOnlyDictionary<Symbol, INodePropertyDef> Properties { get; }
        public IReadOnlyDictionary<Symbol, INodePropertyDef> NodeProperties { get; }
        public IReadOnlyDictionary<Symbol, INodePropertyDef> CollectionNodeProperties { get; }
        public IReadOnlyDictionary<Symbol, INodePropertyDef> FreezableProperties { get; }

        public SimpleNodeTypeDef(Type type) : base(type)
        {
            if (Kind != NodeKind.Simple)
                throw Errors.InternalError("This constructor should be invoked only for ISimpleNode types.");
            
            var properties = new Dictionary<Symbol, INodePropertyDef>();
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
            foreach (var property in type.GetProperties(bindingFlags)) {
                // Must not be 'Key' property
                if (property.Name == nameof(INode.Key))
                    continue;
                // Must have both getter & setter
                if (property.GetGetMethod(false) == null || property.GetSetMethod(false) == null)
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

        public override IEnumerable<KeyValuePair<ItemKey, object?>> GetAllItems(INode node)
        {
            var simpleNode = (ISimpleNode) node;
            foreach (var (name, propertyDef) in Properties) {
                var getter = (Func<ISimpleNode, object>?) propertyDef.UntypedGetter;
                if (getter == null)
                    continue;
                var value = getter.Invoke(simpleNode);
                yield return KeyValuePair.Create((ItemKey) name, value)!;
            }

            var hasOptions = (IHasOptions) simpleNode;
            foreach (var (key, option) in hasOptions.GetAllOptions())
                yield return KeyValuePair.Create((ItemKey) key, option)!;
        }

        public override void GetFreezableItems(INode node, ref ListBuffer<KeyValuePair<ItemKey, IFreezable>> output)
        {
            var simpleNode = (ISimpleNode) node;
            foreach (var (name, propertyDef) in FreezableProperties) {
                var getter = (Func<ISimpleNode, object>?) propertyDef.UntypedGetter;
                if (getter == null)
                    continue;
                var value = getter.Invoke(simpleNode);
                if (value is IFreezable f)
                    output.Add(KeyValuePair.Create((ItemKey) name, f));
            }

            var hasOptions = (IHasOptions) simpleNode;
            foreach (var (key, option) in hasOptions.GetAllOptions()) {
                if (option is IFreezable f)
                    output.Add(KeyValuePair.Create((ItemKey) key, f));
            }
        }

        public override void GetNodeItems(INode node, ref ListBuffer<KeyValuePair<ItemKey, INode>> output)
        {
            var simpleNode = (ISimpleNode) node;
            foreach (var (name, propertyDef) in NodeProperties) {
                var getter = (Func<ISimpleNode, object>?) propertyDef.UntypedGetter;
                if (getter == null)
                    continue;
                var value = getter.Invoke(simpleNode);
                if (value is INode n)
                    output.Add(KeyValuePair.Create((ItemKey) name, n));
            }

            var hasOptions = (IHasOptions) simpleNode;
            foreach (var (key, option) in hasOptions.GetAllOptions()) {
                if (option is INode n)
                    output.Add(KeyValuePair.Create((ItemKey) key, n));
            }
        }

        public override void GetCollectionNodeItems(INode node, ref ListBuffer<KeyValuePair<ItemKey, ICollectionNode>> output)
        {
            var simpleNode = (ISimpleNode) node;
            foreach (var (name, propertyDef) in CollectionNodeProperties) {
                var getter = (Func<ISimpleNode, object>?) propertyDef.UntypedGetter;
                if (getter == null)
                    continue;
                var value = getter.Invoke(simpleNode);
                if (value is ICollectionNode n)
                    output.Add(KeyValuePair.Create((ItemKey) name, n));
            }

            var hasOptions = (IHasOptions) simpleNode;
            foreach (var (key, option) in hasOptions.GetAllOptions()) {
                if (option is ICollectionNode n)
                    output.Add(KeyValuePair.Create((ItemKey) key, n));
            }
        }

        public override bool TryGetItem<T>(INode node, ItemKey itemKey, out T value)
        {
            value = default!;
            var simpleNode = (ISimpleNode) node;
            var symbol = itemKey.AsSymbol(); 
            if (symbol.IsValidOptionsKey()) {
                if (!simpleNode.HasOption(symbol))
                    return false;
                value = simpleNode.GetOption<T>(symbol);
                return true;
            }
            var getter = (Func<ISimpleNode, T>?) Properties[symbol].Getter!;
            if (getter == null)
                return false;
            value = getter.Invoke(simpleNode);
            return true;
        }

        public override bool TryGetItem(INode node, ItemKey itemKey, out object? value)
        {
            value = null;
            var simpleNode = (ISimpleNode) node;
            var symbol = itemKey.AsSymbol(); 
            if (symbol.IsValidOptionsKey()) {
                if (!simpleNode.HasOption(symbol))
                    return false;
                value = simpleNode.GetOption(symbol);
                return true;
            }
            var getter = (Func<ISimpleNode, object?>?) Properties[symbol].UntypedGetter!;
            if (getter == null)
                return false;
            value = getter.Invoke(simpleNode);
            return true;
        }

        public override T GetItem<T>(INode node, ItemKey itemKey)
        {
            var simpleNode = (ISimpleNode) node;
            var symbol = itemKey.AsSymbol(); 
            if (symbol.IsValidOptionsKey())
                return simpleNode.GetOption<T>(symbol);
            var getter = (Func<ISimpleNode, T>) Properties[symbol].Getter!;
            return getter.Invoke(simpleNode);
        }

        public override object? GetItem(INode node, ItemKey itemKey)
        {
            var simpleNode = (ISimpleNode) node;
            var symbol = itemKey.AsSymbol(); 
            if (symbol.IsValidOptionsKey())
                return simpleNode.GetOption(symbol);
            var getter = (Func<ISimpleNode, object?>) Properties[symbol].UntypedGetter!;
            return getter.Invoke(simpleNode);
        }

        public override void SetItem<T>(INode node, ItemKey itemKey, T value)
        {
            var simpleNode = (ISimpleNode) node;
            var symbol = itemKey.AsSymbol(); 
            if (symbol.IsValidOptionsKey()) {
                simpleNode.SetOption(symbol, value);
                return;
            }
            var setter = (Action<ISimpleNode, T>) Properties[symbol].Setter!;
            setter.Invoke(simpleNode, value);
        }

        public override void SetItem(INode node, ItemKey itemKey, object? value)
        {
            var simpleNode = (ISimpleNode) node;
            var symbol = itemKey.AsSymbol(); 
            if (symbol.IsValidOptionsKey()) {
                simpleNode.SetOption(symbol, value);
                return;
            }
            var setter = (Action<ISimpleNode, object?>) Properties[symbol].UntypedSetter!;
            setter.Invoke(simpleNode, value);
        }

        public override void RemoveItem(INode node, ItemKey itemKey)
        {
            var symbol = itemKey.AsSymbol(); 
            symbol.ThrowIfInvalidOptionsKey();
            var simpleNode = (ISimpleNode) node;
            simpleNode.SetOption(symbol, null);
        }
    }
}
