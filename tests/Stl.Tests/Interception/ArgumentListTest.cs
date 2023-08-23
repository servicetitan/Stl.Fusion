using System.Reflection;
using Stl.Interception;

namespace Stl.Tests.Interception;

public class ArgumentListTest(ITestOutputHelper @out) : TestBase(@out)
{
    [Fact]
    public void InvokerTest()
    {
        var type = GetType();
        var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        var l = ArgumentList.Empty;
        var r = l.GetInvoker(type.GetMethod(nameof(VoidMethod0), bindingFlags)!).Invoke(this, l);
        r.Should().BeNull();
        Assert.Throws<ArgumentOutOfRangeException>(
            () => l.GetInvoker(type.GetMethod(nameof(VoidMethod1), bindingFlags)!));

        l = ArgumentList.New(1);
        r = l.GetInvoker(type.GetMethod(nameof(VoidMethod1), bindingFlags)!).Invoke(this, l);
        r.Should().BeNull();
        Assert.Throws<ArgumentOutOfRangeException>(
            () => l.GetInvoker(type.GetMethod(nameof(VoidMethod0), bindingFlags)!));

        r = l.GetInvoker(type.GetMethod(nameof(TaskMethod1), bindingFlags)!).Invoke(this, l);
        r.Should().BeOfType<Task<int>>().Which.Result.Should().Be(1);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => l.GetInvoker(type.GetMethod(nameof(VoidMethod0), bindingFlags)!));

        l = ArgumentList.New(2, 3);
        r = l.GetInvoker(type.GetMethod(nameof(ValueTaskMethod2), bindingFlags)!).Invoke(this, l);
        r.Should().BeOfType<ValueTask<int>>().Which.AsTask().Result.Should().Be(6);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => l.GetInvoker(type.GetMethod(nameof(TaskMethod1), bindingFlags)!));
    }

    private void VoidMethod0()
    {
        Out.WriteLine(nameof(VoidMethod0));
    }

    private void VoidMethod1(int x)
    {
        Out.WriteLine(nameof(VoidMethod1));
    }

    private Task<int> TaskMethod1(int x)
    {
        Out.WriteLine($"{nameof(TaskMethod1)}({x})");
        return Task.FromResult(x);
    }

    private ValueTask<int> ValueTaskMethod2(int x, int y)
    {
        Out.WriteLine($"{nameof(ValueTaskMethod2)}({x}, {y})");
        return new ValueTask<int>(x * y);
    }
}
