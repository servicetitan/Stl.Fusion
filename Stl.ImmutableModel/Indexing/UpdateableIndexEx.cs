namespace Stl.ImmutableModel.Indexing
{
    public static class UpdateableIndexEx 
    {
        public static (TIndex Index, ChangeSet ChangeSet) Update<TIndex>(this TIndex index, INode source, INode target)
            where TIndex : IUpdateableIndex
        {
            var (i, cs) = index.BaseUpdate(source, target);
            return ((TIndex) i, cs);
        }

        public static (TIndex Index, ChangeSet ChangeSet) Update<TIndex>(this TIndex index, INode source, Symbol key, Option<object?> value)
            where TIndex : IUpdateableIndex
            => index.Update(source, source.DualWith(key, value));
        
        public static (TIndex Index, ChangeSet ChangeSet) Update<TIndex>(this TIndex index, SymbolPath path, Option<object?> value)
            where TIndex : IUpdateableIndex
        {
            if (path.Head == null)
                // Root update
                return index.Update(index.GetNode(path), (INode) value.Value!);
            var source = index.GetNode(path.Head);
            var target = source.DualWith(path.Tail, value);
            return index.Update(source, target);
        }
    }
}
