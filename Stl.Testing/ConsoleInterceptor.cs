using System;
using System.IO;
using System.Text;
using System.Threading;
using Stl.Testing.Internal;
using Xunit.Abstractions;

namespace Stl.Testing 
{
    public static class ConsoleInterceptor
    {
        private class ProxyWriter : TextWriter
        {
            public override Encoding Encoding { get; } = Encoding.UTF8;
            public override void Write(char value) => TestOutput.Value?.Write(value);
            public override void Write(string? value) => TestOutput.Value?.Write(value);
        }        

        public static readonly TextWriter TextWriter = new ProxyWriter();
        public static readonly AsyncLocal<TestOutputWriter?> TestOutput = new AsyncLocal<TestOutputWriter?>();
        
        public static Disposable<(TestOutputWriter?, TextWriter)> Activate(ITestOutputHelper testOutput)
        {
            var oldTestOut = TestOutput.Value;
            var oldConsoleOut = Console.Out;
            Console.SetOut(TextWriter);
            TestOutput.Value = new TestOutputWriter(testOutput);
            return Disposable.New(state => {
                var (oldTestOut1, oldConsoleOut1) = state;
                TestOutput.Value = oldTestOut1;
                Console.SetOut(oldConsoleOut1);
            }, (oldTestOut, oldConsoleOut));
        }
        
    }
}
