using System.Security.Cryptography;

namespace Stl.Generators;

// Thread-safe!
public sealed class RandomInt32Generator : Generator<int>
{
    private readonly byte[] _buffer = new byte[sizeof(int)];
    private readonly RandomNumberGenerator _rng;

    public RandomInt32Generator(RandomNumberGenerator? rng = null)
        => _rng = rng ?? RandomNumberGenerator.Create();

    public override int Next()
    {
        lock (_rng) {
            _rng.GetBytes(_buffer);
        }
        var bufferSpan = MemoryMarshal.Cast<byte, int>(_buffer.AsSpan());
        return bufferSpan![0];
    }
}
