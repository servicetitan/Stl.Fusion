using System.CommandLine;
using System.CommandLine.IO;
using FluentAssertions.Primitives;
using Xunit.Abstractions;

namespace Stl.Testing.Output
{
    public class TestConsole : IConsole
    {
        IStandardStreamWriter IStandardOut.Out => Out;
        IStandardStreamWriter IStandardError.Error => Error;
        public TestOutputCapture Out { get; protected set; } = new();
        public TestOutputCapture Error { get; protected set; } = new();

        public bool IsOutputRedirected { get; set; }
        public bool IsErrorRedirected { get; set; }
        public bool IsInputRedirected { get; set; }

        public TestConsole(ITestOutputHelper? testOutput = null)
        {
            if (testOutput != null) {
                var testTextWriter = new TestTextWriter(testOutput);
                Out.Downstream = testTextWriter;
                Error.Downstream = testTextWriter;
            }
        }

        public override string ToString() => Out.ToString();

        public TestConsole Clear()
        {
            Out.Clear();
            Error.Clear();
            return this;
        }

        public StringAssertions Should() => new StringAssertions(ToString());
    }
}
