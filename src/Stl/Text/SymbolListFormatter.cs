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
            var formatter = ListFormat.CreateFormatter();
            foreach (var segment in source.GetSegments())
                formatter.Append(segment.Value);
            formatter.AppendEnd();
            return formatter.Output;
        }

        public SymbolList Parse(string source)
        {
            var parser = ListFormat.CreateParser(source);
            var list = (SymbolList?) null;
            while (parser.TryParseNext())
                list = new SymbolList(list, parser.Item);
            return list ?? SymbolList.Empty;
        }
    }
}
