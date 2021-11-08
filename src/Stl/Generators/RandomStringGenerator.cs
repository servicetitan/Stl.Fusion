using System.Buffers;
using System.Security.Cryptography;
using Stl.Mathematics;

namespace Stl.Generators;

// Thread-safe!
public class RandomStringGenerator : Generator<string>, IDisposable
{
    public static readonly string DefaultAlphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-_";
    public static readonly string Base64Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ+/";
    public static readonly string Base32Alphabet = "0123456789abcdefghijklmnopqrstuv";
    public static readonly string Base16Alphabet = "0123456789abcdef";
    public static readonly RandomStringGenerator Default = new();

    protected readonly RandomNumberGenerator Rng;
    protected object Lock => Rng;

    public string Alphabet { get; }
    public int Length { get; }

    public RandomStringGenerator(int length = 16, string? alphabet = null, RandomNumberGenerator? rng = null)
    {
        if (length < 1)
            throw new ArgumentOutOfRangeException(nameof(length));
        alphabet ??= DefaultAlphabet;
        if (alphabet.Length < 1)
            throw new ArgumentOutOfRangeException(nameof(alphabet));
        rng ??= RandomNumberGenerator.Create();

        Length = length;
        Alphabet = alphabet;
        Rng = rng;
    }

    public void Dispose() => Rng.Dispose();

    public override string Next() => Next(Length);

    private static void FillInCharSpan(Span<char> charSpan, string alphabet, ReadOnlySpan<byte> bufferSpan)
    {
        var alphabetSpan = alphabet.AsSpan();
        var alphabetLength = alphabetSpan.Length;
        if (Bits.IsPowerOf2((uint)alphabetLength)) {
            var alphabetMask = alphabetLength - 1;
            for (var i = 0; i<charSpan.Length; i++)
                charSpan[i] = alphabetSpan[bufferSpan[i] & alphabetMask];
        }
        else {
            for (var i = 0; i<charSpan.Length; i++)
                charSpan[i] = alphabetSpan[bufferSpan[i] % alphabetLength];
        }
    }

    public string Next(int length, string? alphabet = null)
    {
        if (alphabet == null)
            alphabet = Alphabet;
        else if (alphabet.Length < 1)
            throw new ArgumentOutOfRangeException(nameof(alphabet));
#if !NETSTANDARD2_0
        var buffer = MemoryBuffer<byte>.LeaseAndSetCount(false, length);
        try {
            lock (Lock) {
                Rng.GetBytes(buffer.Span);
            }
            return string.Create(length, (buffer.BufferMemory, alphabet), (charSpan, arg) => {
                var (bufferMemory, alphabet1) = arg;
                var bufferSpan = bufferMemory.Span;
                FillInCharSpan(charSpan, alphabet1!, bufferSpan);
            });
        }
        finally {
            buffer.Release();
        }
#else
        var byteArrayPool = ArrayPool<byte>.Shared;
        var charArrayPool = ArrayPool<char>.Shared;
        var byteBuffer = byteArrayPool.Rent(length);
        var charBuffer = charArrayPool.Rent(length);
        try {
            lock (Lock) {
                Rng.GetBytes(byteBuffer, 0, length);
            }
            var charSpan = charBuffer.AsSpan(0, length);
            FillInCharSpan(charSpan, alphabet, byteBuffer.AsSpan());
            return new string(charBuffer, 0, length);
        }
        finally {
            charArrayPool.Return(charBuffer);
            byteArrayPool.Return(byteBuffer);
        }
#endif
    }
}
