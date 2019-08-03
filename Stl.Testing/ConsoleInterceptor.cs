using System;
using System.IO;
using System.Text;
using System.Threading;
using Xunit.Abstractions;

namespace Stl.Testing 
{
    public static class ConsoleInterceptor
    {
        public class ProxyWriter : TextWriter
        {
            public static readonly ProxyWriter Instance = new ProxyWriter();
            public override Encoding Encoding { get; } = Encoding.UTF8;

            private ProxyWriter() {}
            
            public override void Write(char value) => TestOut.Value?.Write(value);
            public override void Write(string? value) => TestOut.Value?.Write(value);
        }        

        public class TestOutWriter : TextWriter
        {
            private static readonly string newLine = Environment.NewLine;
            private static readonly char lastNewLineChar = newLine[newLine.Length - 1];
            public ITestOutputHelper TestOut { get; }
            public StringBuilder Prefix = new StringBuilder();
            public override Encoding Encoding { get; } = Encoding.UTF8;

            public TestOutWriter(ITestOutputHelper testOut) => TestOut = testOut;

            public override void Write(char value)
            {
                if (value == lastNewLineChar)
                    Write(value.ToString());
                else
                    Prefix.Append(value);
            }

            public override void Write(string? value)
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                Prefix.Append(value);
                if (!(value.Contains(newLine) || newLine.EndsWith(value)))
                    return;
                var lines = Prefix.ToString().Split(newLine);
                Prefix.Clear();
                Prefix.Append(lines[lines.Length - 1]);
                for (var i = 0; i < lines.Length - 1; i++) {
                    var line = lines[i];
                    TestOut.WriteLine(line);
                }
            }
        }        

        public static AsyncLocal<TestOutWriter?> TestOut { get; } = new AsyncLocal<TestOutWriter?>();
        
        public static Disposable<(TestOutWriter?, TextWriter)> Activate(ITestOutputHelper testOut)
        {
            var oldTestOut = TestOut.Value;
            var oldConsoleOut = Console.Out;
            Console.SetOut(ProxyWriter.Instance);
            TestOut.Value = new TestOutWriter(testOut);
            return Disposable.New(state => {
                var (oldTestOut1, oldConsoleOut1) = state;
                TestOut.Value = oldTestOut1;
                Console.SetOut(oldConsoleOut1);
            }, (oldTestOut, oldConsoleOut));
        }
        
    }
}
