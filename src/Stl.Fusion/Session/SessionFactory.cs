using Stl.Generators;

namespace Stl.Fusion;

public interface ISessionFactory
{
    public Session CreateSession();
}

public sealed class SessionFactory : ISessionFactory
{
    private readonly Generator<string> _sessionIdGenerator;

    public SessionFactory() : this(new RandomStringGenerator(20)) { }
    public SessionFactory(Generator<string> sessionIdGenerator)
        => _sessionIdGenerator = sessionIdGenerator;

    public Session CreateSession()
        => new(_sessionIdGenerator.Next());
}
