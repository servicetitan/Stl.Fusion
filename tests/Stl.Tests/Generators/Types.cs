using Stl.Interception;

namespace Stl.Tests.Generators;

[GenerateProxy]
public interface ITestInterfaceBase
{
    Task NoArgs();
    Task<int> Foo(int a, Task<bool> b);
}

[GenerateProxy]
public interface ITestInterface : ITestInterfaceBase
{
    Task<int> NoArgs1();
    void Foo2(int a, string b);
}

[GenerateProxy]
public class TestClassBase
{
    public TestClassBase(int x) { }

    public virtual Task BaseNoArgs() => Task.CompletedTask;
    public virtual Task<int> BaseFoo(int a, string b) => Task.FromResult(a);

    // Must be ignored in proxy
    public virtual Task<T> BaseTest1<T>(T argument) => throw new NotSupportedException();
    private Task BaseTest2() => throw new NotSupportedException();
}

[GenerateProxy]
public class TestClass : TestClassBase, ITestInterface
{
    public TestClass(int x) : base(x) { }

    public virtual Task NoArgs() => Task.CompletedTask;
    public virtual Task<int> NoArgs1() => Task.FromResult(0);
    public virtual Task<int> Foo(int a, Task<bool> b) => Task.FromResult(1);
    public virtual void Foo2(int a, string b) { }
    public virtual Task<Type> Foo3(int a) => Task.FromResult(a.GetType());
    public string Boo(string x) => x;

    // Must be ignored in proxy
    public virtual Task<T> Test1<T>(T argument) => throw new NotSupportedException();
    public Task<T> Test2<T>(T argument) => throw new NotSupportedException();
}
