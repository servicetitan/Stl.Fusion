using System;
using System.IO;
using System.Text;
using Xunit.Abstractions;

namespace Stl.Testing.Output
{
    public class TestTextWriter : TextWriter, ITestOutputHelper
    {
        protected static readonly string EnvNewLine = Environment.NewLine;
        protected static readonly char LastEnvNewLineChar = EnvNewLine[^1];
        protected static readonly string LastEnvNewLineString = LastEnvNewLineChar.ToString();

        protected StringBuilder Prefix = new();
        public override Encoding Encoding { get; } = Encoding.UTF8;
        public ITestOutputHelper? Downstream { get; }

        public TestTextWriter(ITestOutputHelper? downstream = null)
            => Downstream = downstream;

        public override void Write(char value)
        {
            if (value == LastEnvNewLineChar)
                Write(value.ToString());
            else
                Prefix.Append(value);
        }

        public override void Write(string? value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            Prefix.Append(value);
#if !NETCOREAPP
            if (!value.Contains(LastEnvNewLineString))
#else
            if (!value.Contains(LastEnvNewLineChar))
#endif
                return;
            var lines = Prefix.ToString().Split(EnvNewLine);
            if (Downstream != null)
                foreach (var line in lines[..^1])
                    Downstream.WriteLine(line);
            Prefix.Clear();
            Prefix.Append(lines[^1]);
        }
    }
}
