using System;
using System.Threading.Tasks;
using Stl.ImmutableModel.Indexing;
using Stl.ImmutableModel.Processing.Internal;

namespace Stl.ImmutableModel.Processing
{
    public interface ITypedNodeProcessor : INodeProcessor
    {
        Type GetNodeType();
    }

    public interface ITypedNodeProcessor<TNode> : ITypedNodeProcessor
        where TNode : class, INode
    { }

    public interface ITypedNodeProcessor<TModel, TNode> : ITypedNodeProcessor<TNode>, INodeProcessor<TModel>
        where TModel : class, INode
        where TNode : class, INode
    { }

    public abstract class TypedNodeProcessorBase<TNode> : NodeProcessorBase, ITypedNodeProcessor<TNode>
        where TNode : class, INode
    {
        protected TypedNodeProcessorBase(IModelProvider modelProvider) 
            : base(modelProvider) { }

        public Type GetNodeType() => typeof(TNode);

        protected override bool IsSupportedChange(in NodeChangeInfo nodeChangeInfo) 
            => nodeChangeInfo.Node is TNode;
    }

    public abstract class TypedNodeProcessorBase<TModel, TNode> : TypedNodeProcessorBase<TNode>, ITypedNodeProcessor<TModel, TNode>
        where TModel : class, INode
        where TNode : class, INode
    {
        public new IModelProvider<TModel> ModelProvider { get; }

        protected TypedNodeProcessorBase(IModelProvider<TModel> modelProvider) : base(modelProvider) 
            => ModelProvider = modelProvider;
    }
}
