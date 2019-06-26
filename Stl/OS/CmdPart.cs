using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Stl.OS 
{
    public abstract class CmdPart
    {
        public static RawCmdPart New(string value) => new RawCmdPart(value);

        // ToString is supposed to render the part into a string
        public ShellQuoteCmdPart ShellQuote() 
            => new ShellQuoteCmdPart(this);
        public OSDependentCmdPart VaryByOS(CmdPart? windowsValue, CmdPart? macOSValue = null)
            => new OSDependentCmdPart(this, windowsValue, macOSValue);

        public static explicit operator string(CmdPart part) => part?.ToString() ?? "";
        public static implicit operator CmdPart(string part) => new RawCmdPart(part);
        public static ConcatCmdPart operator+(CmdPart first, CmdPart second) 
            => new ConcatCmdPart(first, second);
        
        public static ConcatManyCmdPart Concat(params CmdPart[] parts) 
            => new ConcatManyCmdPart(parts);


    }

    public class RawCmdPart : CmdPart
    {
        public string Value { get; }

        public RawCmdPart(string value) => Value = value;

        public override string ToString() => Value;
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

        public override string ToString()
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

        public override string ToString()
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

    public class OSDependentCmdPart : CmdPart
    {
        public CmdPart DefaultValue { get; } 
        public CmdPart WindowsValue { get; } 
        public CmdPart MacOSValue { get; }

        public OSDependentCmdPart(CmdPart defaultValue, 
            CmdPart? windowsValue = null, CmdPart? macOSValue = null)
        {
            DefaultValue = defaultValue;
            WindowsValue = windowsValue ?? defaultValue;
            MacOSValue = macOSValue ?? defaultValue;
        }

        public override string ToString()
        {
            switch (OSInfo.Kind) {
            case OSKind.Windows:
                return (string) WindowsValue;
            case OSKind.MacOS:
                return (string) MacOSValue;
            default:
                return (string) DefaultValue;
            }
        }
    }

    public class ShellQuoteCmdPart : CmdPart
    {
        public CmdPart Value { get; }

        public ShellQuoteCmdPart(CmdPart value) => Value = value;

        public override string ToString()
        {
            switch (OSInfo.Kind) {
            case OSKind.Windows:
                return "\"" + 
                    ((string) Value).Replace("^", "^^").Replace("\"", "^\"") + 
                    "\"";
            default:
                return "'" + 
                    ((string) Value).Replace("'", "'\"'\"'") + 
                    "'";
            }
        }
    }
}
