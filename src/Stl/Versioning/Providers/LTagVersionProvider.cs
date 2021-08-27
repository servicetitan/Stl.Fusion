using Stl.Generators;

namespace Stl.Versioning.Providers
{
    public sealed class LTagVersionProvider : IVersionProvider<LTag>
    {
        public static IVersionProvider<LTag> Default { get; } = new LTagVersionProvider(ConcurrentLTagGenerator.Default);

        private readonly ConcurrentGenerator<LTag> _generator;

        public LTagVersionProvider(ConcurrentGenerator<LTag> generator) => _generator = generator;

        public LTag NextVersion(LTag currentVersion = default)
        {
            while (true) {
                var nextVersion = _generator.Next();
                if (nextVersion != currentVersion)
                    return nextVersion;
            }
        }
    }
}
