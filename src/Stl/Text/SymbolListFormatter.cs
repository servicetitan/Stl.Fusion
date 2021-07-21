namespace Stl.Text
{
    public interface ISymbolListFormatter
    {
        string ToString(SymbolList source);
        SymbolList Parse(string source);
    }

    public class SymbolListFormatter : ISymbolListFormatter
    {
        public static ISymbolListFormatter Default { get; } = new SymbolListFormatter('|');

        public char DelimiterChar { get; }
        public char EscapeChar { get; }
        public string Delimiter { get; }
        public string Escape { get; }
        protected string EscapedDelimiter { get; }
        protected string EscapedEscape { get; }
        protected ListFormat ListFormat { get; }

        public SymbolListFormatter(char delimiter, char escape = '\\')
        {
            DelimiterChar = delimiter;
            EscapeChar = escape;
            Delimiter = delimiter.ToString();
            Escape = escape.ToString();
            EscapedDelimiter = $"{Escape}{Delimiter}";
            EscapedEscape = $"{Escape}{Escape}";
            ListFormat = new ListFormat(delimiter, escape);
        }

        public string ToString(SymbolList source)
        {
            using var f = ListFormat.CreateFormatter();
            foreach (var segment in source.GetSegments())
                f.Append(segment.Value);
            f.AppendEnd();
            return f.Output;
        }

        public SymbolList Parse(string source)
        {
            using var p = ListFormat.CreateParser(source);
            var list = (SymbolList?) null;
            while (p.TryParseNext())
                list = new SymbolList(list, p.Item);
            return list ?? SymbolList.Empty;
        }
    }
}
