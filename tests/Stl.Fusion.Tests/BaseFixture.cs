using AutoFixture;
using AutoFixture.AutoMoq;

namespace Stl.Fusion.Tests;

public abstract class BaseFixture<T>
{
    protected IFixture Fixture { get; }

    protected BaseFixture()
    {
        Fixture = new Fixture()
            .Customize(new AutoMoqCustomization {ConfigureMembers = true});
    }

    public T Create() => Fixture.Create<T>();
}