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
    public class SimpleNodeTypeInfo : NodeTypeInfo
    {
        public IReadOnlyDictionary<Symbol, INodePropertyInfo> Properties { get; }
        public IReadOnlyDictionary<Symbol, INodePropertyInfo> NodeProperties { get; }
        public IReadOnlyDictionary<Symbol, INodePropertyInfo> FreezableProperties { get; }

        public SimpleNodeTypeInfo(Type type) : base(type)
        {
            if (Kind != NodeKind.Simple)
                throw Errors.InternalError("This constructor should be invoked only for ISimpleNode types.");
            
            var properties = new Dictionary<Symbol, INodePropertyInfo>();
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
            foreach (var property in type.GetProperties(bindingFlags)) {
                // Must have both getter & setter
                if (property.GetGetMethod(false) == null || property.GetSetMethod(false) == null)
                    continue;
                var propertyName = new Symbol(property.Name);
                var propertyInfoType = typeof(NodePropertyInfo<>).MakeGenericType(property.PropertyType);
                var propertyInfo = (INodePropertyInfo) Activator.CreateInstance(propertyInfoType, type, propertyName)!;
                properties.Add(propertyName, propertyInfo);
            }

            Properties = new ReadOnlyDictionary<Symbol, INodePropertyInfo>(properties);
            NodeProperties = new ReadOnlyDictionary<Symbol, INodePropertyInfo>(
                properties.Where(p => p.Value.MayBeNode).ToDictionary());
            FreezableProperties = new ReadOnlyDictionary<Symbol, INodePropertyInfo>(
                properties.Where(p => p.Value.MayBeFreezable).ToDictionary());
        }

        public override void FindChildFreezables(INode node, ListBuffer<IFreezable> output)
        {
            var simpleNode = (ISimpleNode) node;
            foreach (var (key, propertyInfo) in FreezableProperties) {
                var getter = (Func<ISimpleNode, object>?) propertyInfo.UntypedGetter;
                if (getter == null)
                    continue;
                var value = getter.Invoke(simpleNode);
                if (value is IFreezable f)
                    output.Add(f);
            }

            var hasOptions = (IHasOptions) simpleNode;
            foreach (var (_, option) in hasOptions) {
                if (option is IFreezable f)
                    output.Add(f);
            }
        }

        public override void FindChildNodes(INode node, ListBuffer<INode> output)
        {
            var simpleNode = (ISimpleNode) node;
            foreach (var (key, propertyInfo) in NodeProperties) {
                var getter = (Func<ISimpleNode, object>?) propertyInfo.UntypedGetter;
                if (getter == null)
                    continue;
                var value = getter.Invoke(simpleNode);
                if (value is INode n)
                    output.Add(n);
            }

            var hasOptions = (IHasOptions) simpleNode;
            foreach (var (_, option) in hasOptions) {
                if (option is INode n)
                    output.Add(n);
            }
        }
    }
}
