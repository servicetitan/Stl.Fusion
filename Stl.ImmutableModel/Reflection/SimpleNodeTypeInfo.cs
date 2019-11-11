using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Stl.Internal;

namespace Stl.ImmutableModel.Reflection
{
    public class SimpleNodeTypeInfo : NodeTypeInfo
    {
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
                var propertyInfo = (INodePropertyInfo) Activator.CreateInstance(propertyInfoType, type, propertyName);
                properties.Add(propertyName, propertyInfo);
            }
            var nodeProperties = properties
                .Where(p => typeof(INode).IsAssignableFrom(p.Value.PropertyInfo.PropertyType))
                .ToDictionary();
            
            Properties = new ReadOnlyDictionary<Symbol, INodePropertyInfo>(properties);
            NodeProperties = new ReadOnlyDictionary<Symbol, INodePropertyInfo>(nodeProperties);
        }
    }
}