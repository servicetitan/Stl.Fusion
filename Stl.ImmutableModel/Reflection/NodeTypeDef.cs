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

        public abstract IEnumerable<KeyValuePair<Symbol, object?>> GetAllItems(INode node);
        public abstract void GetFreezableItems(INode node, ref ListBuffer<KeyValuePair<Symbol, IFreezable>> output);
        public abstract void GetNodeItems(INode node, ref ListBuffer<KeyValuePair<Symbol, INode>> output);
        public abstract void GetCollectionNodeItems(INode node, ref ListBuffer<KeyValuePair<Symbol, ICollectionNode>> output);
        
        public abstract bool TryGetItem<T>(INode node, Symbol localKey, out T value);
        public abstract bool TryGetItem(INode node, Symbol localKey, out object? value);
        public abstract T GetItem<T>(INode node, Symbol localKey);
        public abstract object? GetItem(INode node, Symbol localKey);
        public abstract void SetItem<T>(INode node, Symbol localKey, T value);
        public abstract void SetItem(INode node, Symbol localKey, object? value);
        public abstract void RemoveItem(INode node, Symbol localKey);

        // Useful helpers

        public void GetClosestChildNodesByType<TChild>(INode node, ref ListBuffer<TChild> output, bool includeSelf = false)
            where TChild : class, INode
        {
            if (node is TChild n && includeSelf) {
                output.Add(n);
                return;
            }

            var buffer = ListBuffer<KeyValuePair<Symbol, INode>>.Lease();
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
