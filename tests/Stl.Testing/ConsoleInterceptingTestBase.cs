using System;
using Xunit.Abstractions;

namespace Stl.Testing 
{
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
