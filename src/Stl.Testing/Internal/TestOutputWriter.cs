using System;
using System.IO;
using System.Text;
using Xunit.Abstractions;

namespace Stl.Testing.Internal
{
    public class TestOutputWriter : TextWriter
    {
        protected static readonly string EnvNewLine = Environment.NewLine;

        protected static readonly string LastEnvNewLineString = EnvNewLine[^1].ToString();
        protected static readonly char LastEnvNewLineChar = EnvNewLine[^1];


        protected StringBuilder Prefix = new();
        public ITestOutputHelper TestOutput { get; }
        public override Encoding Encoding { get; } = Encoding.UTF8;

        public TestOutputWriter(ITestOutputHelper testOutput)
            => TestOutput = testOutput;

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
            #if NETSTANDARD2_0
            if (!value.Contains(LastEnvNewLineString))
            #else
            if (!value.Contains(LastEnvNewLineChar))
            #endif
                return;
            var lines = Prefix.ToString().Split(EnvNewLine);
            foreach (var line in lines[..^1])
                TestOutput.WriteLine(line);
            Prefix.Clear();
            Prefix.Append(lines[^1]);
        }
    }

}


#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices
{
    public static class RuntimeHelpers
    {
        public static T[] GetSubArray<T>(T[] array, System.Range range)
        {
            (int offset, int length) = range.GetOffsetAndLength(array.Length);
            var arr = new T[length];
            for (int i = 0; i < length; i++) {
                arr[i] = array[offset + i];
            }
            return arr;
        }
    }
}
#endif