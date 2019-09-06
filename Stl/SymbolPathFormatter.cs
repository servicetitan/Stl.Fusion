using System.Text;

namespace Stl
{
    public interface ISymbolPathFormatter
    {
        string ToString(SymbolPath source);
        SymbolPath Parse(string source);
    }
    
    public class SymbolPathFormatter : ISymbolPathFormatter
    {
        public static ISymbolPathFormatter Default { get; } = new SymbolPathFormatter('/');
        
        public char DelimiterChar { get; }
        public char EscapeChar { get; }
        public string Delimiter { get; }
        public string Escape { get; }
        protected string EscapedDelimiter { get; }
        protected string EscapedEscape { get; }

        public SymbolPathFormatter(char delimiter, char escape = '\\')
        {
            DelimiterChar = delimiter;
            EscapeChar = escape;
            Delimiter = delimiter.ToString();
            Escape = escape.ToString();
            EscapedDelimiter = $"{Escape}{Delimiter}";
            EscapedEscape = $"{Escape}{Escape}";
        }

        public string ToString(SymbolPath source)
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

        public SymbolPath Parse(string source)
        {
            var path = (SymbolPath?) null;
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
                    path = new SymbolPath(path, sb.ToString());
                    sb.Clear();
                }
                else {
                    sb.Append(c);
                }
            }
            if (escape)
                sb.Append(EscapeChar);
            path = new SymbolPath(path, sb.ToString());
            return path;
        }
    }
}
