using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Testing
{
    public abstract class TestBase : IAsyncLifetime
    {
        public ITestOutputHelper Out { get; }

        protected TestBase(ITestOutputHelper @out) => Out = @out;

        public virtual Task InitializeAsync() => Task.CompletedTask;

        public virtual Task DisposeAsync() => Task.CompletedTask;
    }
}
