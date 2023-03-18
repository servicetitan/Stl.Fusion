using Stl.Interception;

namespace Stl.Tests.Generators;

[GenerateProxy]
public interface ITestInterface
{
    Task NoArgs();
    Task<int> NoArgs1();
    Task<int> Foo(int a, string b);
    void Foo3(int a, string b);
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
public class TestClass : TestClassBase
{
    public TestClass(int x) : base(x) { }

    public virtual Task NoArgs() => Task.CompletedTask;
    public virtual Task<int> NoArgs1() => Task.FromResult(0);
    public virtual Task<int> Foo(int a, string b) => Task.FromResult(a);
    public virtual void Foo3(int a, string b) { }
    public virtual Task<System.Type> Foo2(System.Int32 a) => Task.FromResult(a.GetType());
    public string Boo(string x) => x;

    // Must be ignored in proxy
    public virtual Task<T> Test1<T>(T argument) => throw new NotSupportedException();
    public Task<T> Test2<T>(T argument) => throw new NotSupportedException();
}
