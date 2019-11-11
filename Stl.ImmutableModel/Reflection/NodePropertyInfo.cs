using System;
using System.Collections.Generic;
using System.Reflection;
using Stl.ImmutableModel.Internal;
using Stl.Reflection;

namespace Stl.ImmutableModel.Reflection
{
    public interface INodePropertyInfo
    {
        Type Type { get; }
        Symbol PropertyName { get; }
        PropertyInfo PropertyInfo { get; }
        MethodInfo? GetterInfo { get; }
        MethodInfo? SetterInfo { get; }
        Delegate? Getter { get; }
        Delegate? UntypedGetter { get; }
        Delegate? Setter { get; }
        Delegate? UntypedSetter { get; }
    }

    public interface INodePropertyInfo<T> : INodePropertyInfo
    {
        new Func<ISimpleNode, T>? Getter { get; }
        new Func<ISimpleNode, object>? UntypedGetter { get; }
        new Action<ISimpleNode, T>? Setter { get; }
        new Action<ISimpleNode, object>? UntypedSetter { get; }
    }
    
    public class NodePropertyInfo<T> : INodePropertyInfo<T>
    {
        public Type Type { get; }
        public Symbol PropertyName { get; }
        public PropertyInfo PropertyInfo { get; }
        public MethodInfo? GetterInfo { get; }
        public MethodInfo? SetterInfo { get; }
        Delegate? INodePropertyInfo.Getter => Getter;
        Delegate? INodePropertyInfo.UntypedGetter => UntypedGetter;
        Delegate? INodePropertyInfo.Setter => Setter;
        Delegate? INodePropertyInfo.UntypedSetter => UntypedSetter;
        public Func<ISimpleNode, T>? Getter { get; }
        public Func<ISimpleNode, object>? UntypedGetter { get; }
        public Action<ISimpleNode, T>? Setter { get; }
        public Action<ISimpleNode, object>? UntypedSetter { get; }

        public NodePropertyInfo(Type type, Symbol propertyName)
        {
            Type = type;
            PropertyName = propertyName;
            PropertyInfo = PropertyEx.GetProperty(type, propertyName) 
                           ?? throw Errors.PropertyNotFound(type, propertyName);
            GetterInfo = PropertyInfo.GetGetMethod();
            SetterInfo = PropertyInfo.GetSetMethod();
            Getter = (Func<ISimpleNode, T>?) type.GetGetter(propertyName, false);
            UntypedGetter = (Func<ISimpleNode, object>?) type.GetGetter(propertyName, true);
            Setter = (Action<ISimpleNode, T>?) type.GetSetter(propertyName, false);
            UntypedSetter = (Action<ISimpleNode, object>?) type.GetSetter(propertyName, true);
        }
    }
}