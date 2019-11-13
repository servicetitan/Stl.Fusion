using System;
using System.Linq;
using Stl.Collections;
using Stl.Internal;
using Stl.Reflection;

namespace Stl.ImmutableModel.Reflection
{
    public class CollectionNodeTypeInfo : NodeTypeInfo
    {
        public Type ItemType { get; }
        public bool MayItemBeNode { get; } 
        public bool MayItemBeFreezable { get; } 

        public CollectionNodeTypeInfo(Type type) : base(type)
        {
            if (Kind != NodeKind.Collection)
                throw Errors.InternalError("This constructor should be invoked only for ICollectionNode types.");
            
            // Figuring out 
            var tGenericCollectionNode = typeof(ICollectionNode<>);
            var tCollectionInterface = type.GetInterfaces()
                .Single(t => t.IsConstructedGenericType && t.GetGenericTypeDefinition() == tGenericCollectionNode)
            ItemType = tCollectionInterface.GetGenericArguments().Single();

            MayItemBeNode = ItemType.MayCastSucceed(typeof(NodeBase));
            MayItemBeFreezable = ItemType.MayCastSucceed(typeof(IFreezable));
        }

        public override void GetChildFreezables(INode node, ZList<IFreezable> target)
        {
            if (!MayItemBeFreezable) return;
            var collectionNode = (ICollectionNode) node;
            foreach (var value in collectionNode.Values) {
                if (value is IFreezable f)
                    target.Add(f);
            }
        }

        public override void GetChildNodes(INode node, ZList<INode> target)
        {
            if (!MayItemBeNode) return;
            var collectionNode = (ICollectionNode) node;
            foreach (var value in collectionNode.Values) {
                if (value is INode n)
                    target.Add(n);
            }
        }
    }
}
