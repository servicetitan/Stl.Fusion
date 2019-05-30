using System;
using Xunit.Abstractions;

namespace Stl.Testing 
{
    public abstract class TestBase : IDisposable
    {
        public ITestOutputHelper Out { get; }

        protected TestBase(ITestOutputHelper @out) => Out = @out;

        protected virtual void Dispose(bool disposing) {}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public abstract class ConsoleInterceptingTestBase : TestBase
    {
        private IDisposable? _consoleInterceptorDisposable; 

        protected ConsoleInterceptingTestBase(ITestOutputHelper @out) : base(@out)
        {
            // ReSharper disable once HeapView.BoxingAllocation
            _consoleInterceptorDisposable = ConsoleInterceptor.Activate(Out);
        }

        protected override void Dispose(bool disposing)
        {
            _consoleInterceptorDisposable?.Dispose();
            _consoleInterceptorDisposable = null;
        }
    }
}
