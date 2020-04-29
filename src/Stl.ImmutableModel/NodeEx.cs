using Stl.ImmutableModel.Reflection;

namespace Stl.ImmutableModel
{
    public static class NodeEx
    {
        public static NodeTypeDef GetDefinition(this INode node) 
            => NodeTypeDef.Get(node.GetType());
    }
}
