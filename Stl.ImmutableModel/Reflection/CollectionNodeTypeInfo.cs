using System;
using Stl.Internal;

namespace Stl.ImmutableModel.Reflection
{
    public class CollectionNodeTypeInfo : NodeTypeInfo
    {
        public CollectionNodeTypeInfo(Type type) : base(type)
        {
            if (Kind != NodeKind.Collection)
                throw Errors.InternalError("This constructor should be invoked only for ICollectionNode types.");
        }
    }
}