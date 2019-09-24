using Stl.ImmutableModel.Indexing;

namespace Stl.ImmutableModel.Updating
{
    public static class UpdateableIndexEx 
    {
        public static (TIndex Index, ModelChangeSet ChangeSet) Update<TIndex>(this TIndex index, INode source, INode target)
            where TIndex : IUpdatableIndex
        {
            var (i, cs) = index.BaseUpdate(source, target);
            return ((TIndex) i, cs);
        }

        public static (TIndex Index, ModelChangeSet ChangeSet) Update<TIndex>(this TIndex index, INode source, Symbol key, Option<object?> value)
            where TIndex : IUpdatableIndex
            => index.Update(source, source.DualWith(key, value));
        
        public static (TIndex Index, ModelChangeSet ChangeSet) Update<TIndex>(this TIndex index, SymbolList list, Option<object?> value)
            where TIndex : IUpdatableIndex
        {
            if (list.Head == null)
                // Root update
                return index.Update(index.GetNodeByPath(list), (INode) value.Value!);
            var source = index.GetNodeByPath(list.Head);
            var target = source.DualWith(list.Tail, value);
            return index.Update(source, target);
        }
    }
}
