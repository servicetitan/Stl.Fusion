using System;
using System.Text;

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
        protected ListParser ListParser { get; }

        public SymbolListFormatter(char delimiter, char escape = '\\')
        {
            DelimiterChar = delimiter;
            EscapeChar = escape;
            Delimiter = delimiter.ToString();
            Escape = escape.ToString();
            EscapedDelimiter = $"{Escape}{Delimiter}";
            EscapedEscape = $"{Escape}{Escape}";
            ListParser = new ListParser(delimiter, escape);
        }

        public string ToString(SymbolList source)
        {
            var sb = new StringBuilder();
            var index = 0;
            foreach (var segment in source.GetSegments())
                ListParser.FormatItem(sb, segment.Value, ref index);
            return sb.ToString();
        }

        public SymbolList Parse(string source)
        {
            var list = (SymbolList?) null;
            var tail = source.AsSpan();
            var index = 0;
            var item = new StringBuilder();
            while (ListParser.ParseItem(ref tail, ref index, item)) {
                list = new SymbolList(list, item.ToString());
                item.Clear();
            }
            return list ?? SymbolList.Empty;
        }
    }
}
