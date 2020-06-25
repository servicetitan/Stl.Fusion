namespace Stl.Text 
{
    public static class SymbolListEx
    {
        public static bool IsEmpty(this SymbolList symbolList)
            => SymbolList.Empty.Equals(symbolList);
    }
}
