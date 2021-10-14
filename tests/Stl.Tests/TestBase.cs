using System;
using System.Text;
using System.Threading.Tasks;
using Stl.Testing.Output;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests;

public abstract class TestBase : IAsyncLifetime
{
    public ITestOutputHelper Out { get; set; }

    protected TestBase(ITestOutputHelper @out) => Out = @out;

    public virtual Task InitializeAsync() => Task.CompletedTask;
    public virtual Task DisposeAsync() => Task.CompletedTask;

    protected Disposable<TestOutputCapture> CaptureOutput()
    {
        var testOutputCapture = new TestOutputCapture(Out);
        var oldOut = Out;
        Out = testOutputCapture;
        return new Disposable<TestOutputCapture>(
            testOutputCapture,
            _ => Out = oldOut);
    }

}
