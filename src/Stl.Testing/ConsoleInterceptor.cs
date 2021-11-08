using System.Text;
using Stl.Testing.Output;
using Xunit.Abstractions;

namespace Stl.Testing;

public static class ConsoleInterceptor
{
    private class ProxyWriter : TextWriter
    {
        public override Encoding Encoding { get; } = Encoding.UTF8;
        public override void Write(char value) => TestOutput.Value?.Write(value);
        public override void Write(string? value) => TestOutput.Value?.Write(value);
    }

    public static readonly TextWriter TextWriter = new ProxyWriter();
    public static readonly AsyncLocal<TestTextWriter?> TestOutput = new AsyncLocal<TestTextWriter?>();

    public static ClosedDisposable<(TestTextWriter?, TextWriter)> Activate(ITestOutputHelper testOutput)
    {
        var oldTestOut = TestOutput.Value;
        var oldConsoleOut = Console.Out;
        Console.SetOut(TextWriter);
        TestOutput.Value = new TestTextWriter(testOutput);
        return Disposable.NewClosed((oldTestOut, oldConsoleOut), state => {
            var (oldTestOut1, oldConsoleOut1) = state;
            TestOutput.Value = oldTestOut1;
            Console.SetOut(oldConsoleOut1);
        });
    }

}
