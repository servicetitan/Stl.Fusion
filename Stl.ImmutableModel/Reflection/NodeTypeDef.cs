using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stl.Collections;
using Stl.ImmutableModel.Internal;
using Stl.Reflection;

namespace Stl.ImmutableModel.Reflection
{
    public abstract class NodeTypeDef
    {
        private static readonly ConcurrentDictionary<Type, NodeTypeDef> _cache =
            new ConcurrentDictionary<Type, NodeTypeDef>();

        public Type Type { get; }
        public NodeKind Kind { get; protected set; }

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
                    .Select(t => t.GetMethod(nameof(SimpleNodeBase.CreateNodeTypeDef), bindingFlags))
                    .FirstOrDefault(mi => mi != null);
                if (methodInfo == null)
                    throw Errors.CannotCreateNodeTypeDef(type);
                r = (NodeTypeDef) methodInfo.Invoke(null, new object?[] {type})!;
                _cache[type] = r;
                return r;
            }
        }

        protected NodeTypeDef(Type type)
        {
            Type = type;
            Kind = typeof(ICollectionNode).IsAssignableFrom(type) ? NodeKind.Collection : NodeKind.Simple;
        }

        public abstract IEnumerable<KeyValuePair<ItemKey, object?>> GetAllItems(INode node);
        public abstract void GetFreezableItems(INode node, ref ListBuffer<KeyValuePair<ItemKey, IFreezable>> output);
        public abstract void GetNodeItems(INode node, ref ListBuffer<KeyValuePair<ItemKey, INode>> output);
        public abstract void GetCollectionNodeItems(INode node, ref ListBuffer<KeyValuePair<ItemKey, ICollectionNode>> output);
        
        public abstract bool TryGetItem<T>(INode node, ItemKey itemKey, out T value);
        public abstract bool TryGetItem(INode node, ItemKey itemKey, out object? value);
        public abstract T GetItem<T>(INode node, ItemKey itemKey);
        public abstract object? GetItem(INode node, ItemKey itemKey);
        public abstract void SetItem<T>(INode node, ItemKey itemKey, T value);
        public abstract void SetItem(INode node, ItemKey itemKey, object? value);
        public abstract void RemoveItem(INode node, ItemKey itemKey);

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
