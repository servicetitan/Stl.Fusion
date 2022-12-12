using Stl.Interception;

namespace Stl.Fusion.Tests.Generators;

[GenerateProxy]
public interface IChats
{
    Task<int> Foo(int a, string b);

    void Foo3(int a, string b);
}

[GenerateProxy]
public class Chats : IChats
{
    public virtual Task<int> Foo(int a, string b)
    {
        return Task.FromResult(a);
    }

    public virtual void Foo3(int a, string b)
    {
    }

    public virtual Task<System.Type> Foo2(System.Int32 a)
    {
        return Task.FromResult(a.GetType());
    }

    public string Boo(string x)
    {
        return x;
    }

    public Chats(int x)
    {
    }
}