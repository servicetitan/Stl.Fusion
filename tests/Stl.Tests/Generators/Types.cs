using Stl.Interception;

namespace Stl.Tests.Generators;

public interface ITestInterfaceBase : IRequiresFullProxy
{
    Task Proxy1();
    Task<int> Proxy2(int a, Task<bool> b);
}

public interface ITestInterface : ITestInterfaceBase
{
    Task<int> Proxy3();
    void Proxy4(int a, string b);
}

public class TestClassBase : IRequiresAsyncProxy
{
    public TestClassBase(int x) { }

    public virtual Task Proxy5() => Task.CompletedTask;
    public virtual Task<int> Proxy6(int a, string b) => Task.FromResult(a);

    // Must be ignored in proxy
    public virtual int NoProxy1(int a, string b) => 1; // Non-async
    public virtual Task<T> NoProxy2<T>(T argument) => throw new NotSupportedException(); // Generic
    private Task NoProxy3() => throw new NotSupportedException(); // Private
    [ProxyIgnore]
    public virtual Task<int> NoProxy4(int a) => Task.FromResult(a);
}

public class TestClass : TestClassBase, ITestInterface
{
    public TestClass(int x) : base(x) { }

    public virtual Task Proxy1() => Task.CompletedTask;
    public virtual Task<int> Proxy2(int a, Task<bool> b) => Task.FromResult(1);
    public virtual Task<int> Proxy3() => Task.FromResult(0);
    public virtual void Proxy4(int a, string b) { }
    public virtual Task<Type> Proxy7(int a) => Task.FromResult(a.GetType());
    public string Proxy8(string x) => x;

    // Must be ignored in proxy
    public virtual Task<T> NoProxyA1<T>(T argument) => throw new NotSupportedException();
    public Task<T> NoProxyA2<T>(T argument) => throw new NotSupportedException();
    public override Task<int> NoProxy4(int a) => Task.FromResult(a);
}

public interface IInterfaceProxy : IRequiresFullProxy
{
    void VoidMethod();
    Task Method0();
    Task Method1(CancellationToken cancellationToken);
    Task Method2(int x, CancellationToken cancellationToken);
}

public class ClassProxy : IInterfaceProxy
{
    public virtual void VoidMethod() { }
    public virtual Task Method0() => Task.CompletedTask;
    public virtual Task Method1(CancellationToken cancellationToken) => Task.CompletedTask;
    public virtual Task Method2(int x, CancellationToken cancellationToken) => Task.CompletedTask;
}
