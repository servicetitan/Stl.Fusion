using System.CommandLine.IO;
using System.Text;
using FluentAssertions.Primitives;
using Xunit.Abstractions;

namespace Stl.Testing.Output;

public class TestOutputCapture : IStandardStreamWriter, ITestOutputHelper
{
    protected object Lock = new();
    public StringBuilder StringBuilder = new();
    public TestTextWriter? Downstream { get; set; }

    public TestOutputCapture(TestTextWriter? downstream = null)
        => Downstream = downstream;
    public TestOutputCapture(ITestOutputHelper downstream)
        => Downstream = new TestTextWriter(downstream);

    public override string ToString()
    {
        lock (Lock) {
            return StringBuilder.ToString();
        }
    }

    public void WriteLine(string message)
        => Write(message + Environment.NewLine);

    public void WriteLine(string format, params object[] args)
        => WriteLine(string.Format(format, args));

    public void Write(char value)
    {
        lock (Lock) {
            StringBuilder.Append(value);
            Downstream?.Write(value);
        }
    }

    public void Write(string value)
    {
        lock (Lock) {
            StringBuilder.Append(value);
            Downstream?.Write(value);
        }
    }

    public TestOutputCapture Clear()
    {
        lock (Lock) {
            StringBuilder.Clear();
            return this;
        }
    }

    public StringAssertions Should() => new(ToString());
}
