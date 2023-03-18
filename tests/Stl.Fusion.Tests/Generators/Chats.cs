using Stl.Interception;

namespace Stl.Fusion.Tests.Generators;

[GenerateProxy]
public interface IChats
{
    Task NoArgs();
    Task<int> NoArgs1();
    Task<int> Foo(int a, string b);
    void Foo3(int a, string b);
}

[GenerateProxy]
public class Chats : IChats
{
    public Chats(int x) { }

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
