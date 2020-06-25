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
}
