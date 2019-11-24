using System;
using System.Reflection;
using Stl.ImmutableModel.Internal;
using Stl.Reflection;
using Stl.Text;

namespace Stl.ImmutableModel.Reflection
{
    public interface INodePropertyDef
    {
        Type Type { get; }
        Symbol Name { get; }
        PropertyInfo PropertyInfo { get; }

        bool IsFreezable { get; }
        bool IsNode { get; }
        bool IsCollectionNode { get; }
        bool MayBeFreezable { get; } 
        bool MayBeNode { get; }
        bool MayBeCollectionNode { get; }

        MethodInfo? GetterInfo { get; }
        MethodInfo? SetterInfo { get; }
        Delegate? Getter { get; }
        Delegate? UntypedGetter { get; }
        Delegate? Setter { get; }
        Delegate? UntypedSetter { get; }
    }

    public interface INodePropertyDef<T> : INodePropertyDef
    {
        new Func<INode, T>? Getter { get; }
        new Func<INode, object?>? UntypedGetter { get; }
        new Action<INode, T>? Setter { get; }
        new Action<INode, object?>? UntypedSetter { get; }
    }
    
    public class NodePropertyDef<T> : INodePropertyDef<T>
    {
        public Type Type { get; }
        public Symbol Name { get; }
        public PropertyInfo PropertyInfo { get; }

        public bool IsFreezable { get; }
        public bool IsNode { get; }
        public bool IsCollectionNode { get; }
        public bool MayBeFreezable { get; }
        public bool MayBeNode { get; }
        public bool MayBeCollectionNode { get; }

        public MethodInfo? GetterInfo { get; }
        public MethodInfo? SetterInfo { get; }
        Delegate? INodePropertyDef.Getter => Getter;
        Delegate? INodePropertyDef.UntypedGetter => UntypedGetter;
        Delegate? INodePropertyDef.Setter => Setter;
        Delegate? INodePropertyDef.UntypedSetter => UntypedSetter;
        public Func<INode, T>? Getter { get; }
        public Func<INode, object?>? UntypedGetter { get; }
        public Action<INode, T>? Setter { get; }
        public Action<INode, object?>? UntypedSetter { get; }

        public NodePropertyDef(Type type, Symbol propertyName)
        {
            Type = type;
            Name = propertyName;
            PropertyInfo = PropertyEx.GetProperty(type, propertyName) 
                           ?? throw Errors.PropertyNotFound(type, propertyName);
            var propertyType = PropertyInfo.PropertyType;

            IsFreezable = typeof(IFreezable).IsAssignableFrom(propertyType); 
            IsNode = typeof(Node).IsAssignableFrom(propertyType); 
            IsCollectionNode = typeof(CollectionNodeBase).IsAssignableFrom(propertyType); 
            MayBeFreezable = propertyType.MayCastSucceed(typeof(IFreezable));
            MayBeNode = propertyType.MayCastSucceed(typeof(Node));
            MayBeCollectionNode = propertyType.MayCastSucceed(typeof(CollectionNodeBase));

            GetterInfo = PropertyInfo.GetGetMethod();
            SetterInfo = PropertyInfo.GetSetMethod();
            Getter = (Func<INode, T>?) type.GetGetter(propertyName, false);
            UntypedGetter = (Func<INode, object?>?) type.GetGetter(propertyName, true);
            Setter = (Action<INode, T>?) type.GetSetter(propertyName, false);
            UntypedSetter = (Action<INode, object?>?) type.GetSetter(propertyName, true);
        }
    }
}
