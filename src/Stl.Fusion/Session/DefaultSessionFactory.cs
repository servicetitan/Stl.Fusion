using Stl.Generators;

namespace Stl.Fusion;

public sealed class DefaultSessionFactory
{
    public static SessionFactory New(int length = 20, string? alphabet = null)
        => New(new RandomStringGenerator(length, alphabet));

    public static SessionFactory New(Generator<string> generator)
        => () => new Session(generator.Next());
}
