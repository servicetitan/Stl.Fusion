using System.CommandLine;
using FluentAssertions.Primitives;
using Stl.Testing.Internal;
using Xunit.Abstractions;

namespace Stl.Testing
{
    public class TestOutputConsole : IConsole
    {
        IStandardStreamWriter IStandardOut.Out => Out;
        IStandardStreamWriter IStandardError.Error => Error;
        public TestStreamWriter Out { get; protected set; } = new TestStreamWriter();
        public TestStreamWriter Error { get; protected set; } = new TestStreamWriter();

        public bool IsOutputRedirected { get; set; }
        public bool IsErrorRedirected { get; set; }
        public bool IsInputRedirected { get; set; }

        public TestOutputConsole(ITestOutputHelper? testOutput = null)
        {
            if (testOutput != null) {
                var wrapper = new TestOutputWriter(testOutput);
                Out.TextWriter = wrapper;
                Error.TextWriter = wrapper;
            }
        }

        public override string ToString() => Out.ToString();

        public TestOutputConsole Clear()
        {
            Out.Clear();
            Error.Clear();
            return this;
        }

        public StringAssertions Should() => new StringAssertions(ToString());
    }
}
