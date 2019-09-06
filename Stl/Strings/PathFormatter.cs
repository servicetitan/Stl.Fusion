using System.Text;

namespace Stl.Strings
{
    public interface IPathFormatter
    {
        string ToString(Path source);
        Path Parse(string source);
    }
    
    public class PathFormatter : IPathFormatter
    {
        public static IPathFormatter Default { get; } = new PathFormatter('/');
        
        public char DelimiterChar { get; }
        public char EscapeChar { get; }
        public string Delimiter { get; }
        public string Escape { get; }
        protected string EscapedDelimiter { get; }
        protected string EscapedEscape { get; }

        public PathFormatter(char delimiter, char escape = '\\')
        {
            DelimiterChar = delimiter;
            EscapeChar = escape;
            Delimiter = delimiter.ToString();
            Escape = escape.ToString();
            EscapedDelimiter = $"{Escape}{Delimiter}";
            EscapedEscape = $"{Escape}{Escape}";
        }

        public string ToString(Path source)
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

        public Path Parse(string source)
        {
            var path = (Path?) null;
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
                    path = new Path(path, sb.ToString());
                    sb.Clear();
                }
                else {
                    sb.Append(c);
                }
            }
            if (escape)
                sb.Append(EscapeChar);
            path = new Path(path, sb.ToString());
            return path;
        }
    }
}