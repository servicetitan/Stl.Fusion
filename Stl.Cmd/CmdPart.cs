using System.Text;

namespace Stl.Cmd 
{
    public abstract class CmdPart
    {
        public abstract string Render();

        public override string ToString() => Render();

        public static explicit operator string(CmdPart part) => part?.ToString() ?? "";
        public static implicit operator CmdPart(string part) => new RawCmdPart(part);
        public static CmdPart operator+(CmdPart first, CmdPart second) => new ConcatCmdPart(first, second);
        public static CmdPart Concat(params CmdPart[] parts) => new ConcatManyCmdPart(parts);
    }

    public class RawCmdPart : CmdPart
    {
        public string Value { get; }

        public RawCmdPart(string value) => Value = value;

        public override string Render() => Value;
    }

    public sealed class ConcatCmdPart : CmdPart
    {
        public CmdPart First { get; }
        public CmdPart Second { get; }

        public ConcatCmdPart(CmdPart first, CmdPart second)
        {
            First = first;
            Second = second;
        }

        public override string Render()
        {
            var first = (string) First;
            var second = (string) Second;
            return string.IsNullOrEmpty(second)
                ? first
                : $"{first} {second}";
        }
    }

    public sealed class ConcatManyCmdPart : CmdPart
    {
        public CmdPart[] Parts { get; }

        public ConcatManyCmdPart(CmdPart[] parts) => Parts = parts;

        public override string Render()
        {
            var sb = new StringBuilder();
            var prefix = "";
            foreach (var part in Parts) {
                var rendered = (string) part;
                if (string.IsNullOrEmpty(rendered))
                    continue;
                sb.Append(prefix);
                sb.Append(rendered);
                prefix = " ";
            }
            return sb.ToString();
        }
    }
}
