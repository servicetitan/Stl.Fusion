using Stl.Generators;

namespace Stl.Fusion.Authentication
{
    public interface ISessionFactory
    {
        public Session CreateSession();
    }

    public class SessionFactory : ISessionFactory
    {
        protected Generator<string> SessionIdGenerator { get; }

        public SessionFactory() : this(RandomStringGenerator.Default) { }
        public SessionFactory(Generator<string> sessionIdGenerator)
            => SessionIdGenerator = sessionIdGenerator;

        public virtual Session CreateSession()
            => new(SessionIdGenerator.Next());
    }
}
