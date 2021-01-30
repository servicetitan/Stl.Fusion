using System;
using System.Threading.Tasks;
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

        public override Task DisposeAsync()
        {
            _consoleInterceptorDisposable?.Dispose();
            _consoleInterceptorDisposable = null;
            return Task.CompletedTask;
        }
    }
}
