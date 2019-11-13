namespace Stl.ImmutableModel 
{
    public static class KeyEx
    {
        private static readonly Symbol UnspecifiedKeyHead = Key.Unspecified.Parts.Head;
        
        public static bool IsRoot(this Key key) => SymbolList.Root.Equals(key.Parts);
        public static bool IsUnspecified(this Key key) => UnspecifiedKeyHead.Equals(key.Parts.Head);
    }
}
