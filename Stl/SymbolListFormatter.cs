using System.Text;

namespace Stl
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

        public SymbolListFormatter(char delimiter, char escape = '\\')
        {
            DelimiterChar = delimiter;
            EscapeChar = escape;
            Delimiter = delimiter.ToString();
            Escape = escape.ToString();
            EscapedDelimiter = $"{Escape}{Delimiter}";
            EscapedEscape = $"{Escape}{Escape}";
        }

        public string ToString(SymbolList source)
        {
            var sb = new StringBuilder();
            var needsDelimiter = false;
            foreach (var segment in source.GetSegments()) {
                if (needsDelimiter)
                    sb.Append(Delimiter);
                else 
                    needsDelimiter = true;
                sb.Append(segment.Value
                    .Replace(Escape, EscapedEscape)
                    .Replace(Delimiter, EscapedDelimiter));
            }
            return sb.ToString();
        }

        public SymbolList Parse(string source)
        {
            var list = (SymbolList?) null;
            var sb = new StringBuilder();
            var escape = false;
            foreach (var c in source) {
                if (escape) {
                    sb.Append(c);
                    escape = false;
                }
                else if (c == EscapeChar) {
                    escape = true;
                }
                else if (c == DelimiterChar) {
                    list = new SymbolList(list, sb.ToString());
                    sb.Clear();
                }
                else {
                    sb.Append(c);
                }
            }
            if (escape)
                sb.Append(EscapeChar);
            list = new SymbolList(list, sb.ToString());
            return list;
        }
    }
}
