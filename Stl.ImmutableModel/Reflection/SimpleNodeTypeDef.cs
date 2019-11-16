using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Stl.Collections;
using Stl.Extensibility;
using Stl.Internal;

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
                var propertyName = new Symbol(property.Name);
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

        public override IEnumerable<KeyValuePair<Symbol, object?>> GetAllItems(INode node)
        {
            var simpleNode = (ISimpleNode) node;
            foreach (var (key, propertyInfo) in Properties) {
                var getter = (Func<ISimpleNode, object>?) propertyInfo.UntypedGetter;
                if (getter == null)
                    continue;
                var value = getter.Invoke(simpleNode);
                yield return KeyValuePair.Create(key, value)!;
            }

            var hasOptions = (IHasOptions) simpleNode;
            foreach (var (key, option) in hasOptions)
                yield return KeyValuePair.Create(key, option)!;
        }

        public override void GetFreezableItems(INode node, ListBuffer<KeyValuePair<Symbol, IFreezable>> output)
        {
            var simpleNode = (ISimpleNode) node;
            foreach (var (key, propertyInfo) in FreezableProperties) {
                var getter = (Func<ISimpleNode, object>?) propertyInfo.UntypedGetter;
                if (getter == null)
                    continue;
                var value = getter.Invoke(simpleNode);
                if (value is IFreezable f)
                    output.Add(KeyValuePair.Create(key, f));
            }

            var hasOptions = (IHasOptions) simpleNode;
            foreach (var (key, option) in hasOptions) {
                if (option is IFreezable f)
                    output.Add(KeyValuePair.Create(key, f));
            }
        }

        public override void GetNodeItems(INode node, ListBuffer<KeyValuePair<Symbol, INode>> output)
        {
            var simpleNode = (ISimpleNode) node;
            foreach (var (key, propertyInfo) in NodeProperties) {
                var getter = (Func<ISimpleNode, object>?) propertyInfo.UntypedGetter;
                if (getter == null)
                    continue;
                var value = getter.Invoke(simpleNode);
                if (value is INode n)
                    output.Add(KeyValuePair.Create<Symbol, INode>(key, n));
            }

            var hasOptions = (IHasOptions) simpleNode;
            foreach (var (key, option) in hasOptions) {
                if (option is INode n)
                    output.Add(KeyValuePair.Create<Symbol, INode>(key, n));
            }
        }

        public override void GetCollectionNodeItems(INode node, ListBuffer<KeyValuePair<Symbol, ICollectionNode>> output)
        {
            var simpleNode = (ISimpleNode) node;
            foreach (var (key, propertyInfo) in CollectionNodeProperties) {
                var getter = (Func<ISimpleNode, object>?) propertyInfo.UntypedGetter;
                if (getter == null)
                    continue;
                var value = getter.Invoke(simpleNode);
                if (value is ICollectionNode n)
                    output.Add(KeyValuePair.Create<Symbol, ICollectionNode>(key, n));
            }

            var hasOptions = (IHasOptions) simpleNode;
            foreach (var (key, option) in hasOptions) {
                if (option is ICollectionNode n)
                    output.Add(KeyValuePair.Create<Symbol, ICollectionNode>(key, n));
            }
        }

        public override bool TryGetItem<T>(INode node, Symbol localKey, out T value)
        {
            value = default!;
            var simpleNode = (ISimpleNode) node;
            if (localKey.IsValidOptionsKey()) {
                if (!simpleNode.HasOption(localKey))
                    return false;
                value = simpleNode.GetOption<T>(localKey);
                return true;
            }
            var getter = (Func<ISimpleNode, T>?) Properties[localKey].Getter!;
            if (getter == null)
                return false;
            value = getter.Invoke(simpleNode);
            return true;
        }

        public override bool TryGetItem(INode node, Symbol localKey, out object? value)
        {
            value = null;
            var simpleNode = (ISimpleNode) node;
            if (localKey.IsValidOptionsKey()) {
                if (!simpleNode.HasOption(localKey))
                    return false;
                value = simpleNode.GetOption(localKey);
                return true;
            }
            var getter = (Func<ISimpleNode, object?>?) Properties[localKey].UntypedGetter!;
            if (getter == null)
                return false;
            value = getter.Invoke(simpleNode);
            return true;
        }

        public override T GetItem<T>(INode node, Symbol localKey)
        {
            var simpleNode = (ISimpleNode) node;
            if (localKey.IsValidOptionsKey())
                return simpleNode.GetOption<T>(localKey);
            var getter = (Func<ISimpleNode, T>) Properties[localKey].Getter!;
            return getter.Invoke(simpleNode);
        }

        public override object? GetItem(INode node, Symbol localKey)
        {
            var simpleNode = (ISimpleNode) node;
            if (localKey.IsValidOptionsKey())
                return simpleNode.GetOption(localKey);
            var getter = (Func<ISimpleNode, object?>) Properties[localKey].UntypedGetter!;
            return getter.Invoke(simpleNode);
        }

        public override void SetItem<T>(INode node, Symbol localKey, T value)
        {
            var simpleNode = (ISimpleNode) node;
            if (localKey.IsValidOptionsKey()) {
                simpleNode.SetOption(localKey, value);
                return;
            }
            var setter = (Action<ISimpleNode, T>) Properties[localKey].Setter!;
            setter.Invoke(simpleNode, value);
        }

        public override void SetItem(INode node, Symbol localKey, object? value)
        {
            var simpleNode = (ISimpleNode) node;
            if (localKey.IsValidOptionsKey()) {
                simpleNode.SetOption(localKey, value);
                return;
            }
            var setter = (Action<ISimpleNode, object?>) Properties[localKey].UntypedSetter!;
            setter.Invoke(simpleNode, value);
        }

        public override void RemoveItem(INode node, Symbol localKey)
        {
            localKey.ThrowIfInvalidOptionsKey();
            var simpleNode = (ISimpleNode) node;
            simpleNode.SetOption(localKey, null);
        }
    }
}
