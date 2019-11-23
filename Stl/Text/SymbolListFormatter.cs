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
        protected ListFormatHelper ListFormatHelper { get; }

        public SymbolListFormatter(char delimiter, char escape = '\\')
        {
            DelimiterChar = delimiter;
            EscapeChar = escape;
            Delimiter = delimiter.ToString();
            Escape = escape.ToString();
            EscapedDelimiter = $"{Escape}{Delimiter}";
            EscapedEscape = $"{Escape}{Escape}";
            ListFormatHelper = new ListFormatHelper(delimiter, escape);
        }

        public string ToString(SymbolList source)
        {
            var formatter = ListFormatHelper.CreateFormatter();
            foreach (var segment in source.GetSegments())
                formatter.AddItem(segment.Value);
            formatter.AddEnd();
            return formatter.Output;
        }

        public SymbolList Parse(string source)
        {
            var parser = ListFormatHelper.CreateParser(source);
            var list = (SymbolList?) null;
            while (parser.ClearAndParseItem()) {
                list = new SymbolList(list, parser.Item);
            }
            return list ?? SymbolList.Empty;
        }
    }
}
