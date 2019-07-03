using System;
using System.CommandLine;
using System.Text;
using Stl.OS;

namespace Stl.CommandLine 
{
    public abstract class CmdPart
    {
        public static RawCmdPart New(string value) => new RawCmdPart(value);

        // Quotations
        public static string UnixQuote(string value) => 
            "'" + value.Replace("'", "'\"'\"'") + "'";
        public static string WindowsQuote(string value) => 
            "\"" + value.Replace("^", "^^").Replace("\"", "^\"") + "\"";
        public static string Quote(string value) =>
            OSInfo.Kind switch {
                OSKind.Windows => WindowsQuote(value),
                _ => UnixQuote(value)
            };

        // ToString is supposed to render the part into a string
        public QuotedCmdPart Quote() 
            => new QuotedCmdPart(this);
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

        public override string ToString() =>
            OSInfo.Kind switch {
                OSKind.Windows => (string) WindowsValue,
                OSKind.MacOS => (string) MacOSValue,
                _ => (string) DefaultValue
            };
    }

    public class QuotedCmdPart : CmdPart
    {
        public CmdPart Value { get; }
        public QuotedCmdPart(CmdPart value) => Value = value;
        public override string ToString() => Quote((string) Value);
    }

    public class ArgumentCmdPart : CmdPart
    {
        public Argument Argument { get; }
        public ArgumentCmdPart(Argument argument) => Argument = argument;
//        public override string ToString() => Quote((string) Argument);
    }
}
