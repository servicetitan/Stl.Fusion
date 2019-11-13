using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Stl.Collections;
using Stl.ImmutableModel.Internal;
using Stl.Reflection;

namespace Stl.ImmutableModel.Reflection
{
    public abstract class NodeTypeInfo
    {
        private static readonly ConcurrentDictionary<Type, NodeTypeInfo> _cache =
            new ConcurrentDictionary<Type, NodeTypeInfo>();

        public Type Type { get; }
        public NodeKind Kind { get; protected set; }

        public static NodeTypeInfo Get(Type type)
        {
            if (_cache.TryGetValue(type, out var r))
                return r;
            lock (_cache) {
                if (_cache.TryGetValue(type, out r))
                    return r;
                var bases = EnumerableEx.One(type).Concat(type.GetAllBaseTypes());
                var bindingFlags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                var mi = bases
                    .Select(t => t.GetMethod(nameof(SimpleNodeBase.CreateNodeTypeInfo), bindingFlags))
                    .FirstOrDefault();
                if (mi == null)
                    throw Errors.CannotCreateNodeTypeInfo(type);
                r = (NodeTypeInfo) mi.Invoke(null, new object?[] {type})!;
                _cache[type] = r;
                return r;
            }
        }

        protected NodeTypeInfo(Type type)
        {
            Type = type;
            Kind = typeof(ICollectionNode).IsAssignableFrom(type) ? NodeKind.Collection : NodeKind.Simple;
        }

        public abstract void FindChildFreezables(INode node, ListBuffer<IFreezable> output);
        public abstract void FindChildNodes(INode node, ListBuffer<INode> output);
    }
}
