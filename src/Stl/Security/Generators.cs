using System;
using System.Security.Cryptography;
using System.Threading;
using Stl.Collections;
using Stl.Mathematics;
using Stl.Text;

namespace Stl.Security
{
    public interface IGenerator<out T>
    {
        T Next();
    }

    public sealed class Int32Generator : IGenerator<int>
    {
        public static readonly Int32Generator Default = new Int32Generator();

        private int _counter;

        public Int32Generator(int start = 1)
            => _counter = start - 1;

        public int Next()
            => Interlocked.Increment(ref _counter);
    }

    public sealed class Int64Generator : IGenerator<long>
    {
        public static readonly Int64Generator Default = new Int64Generator();

        private long _counter;

        public Int64Generator(long start = 1)
            => _counter = start - 1;

        public long Next()
            => Interlocked.Increment(ref _counter);
    }

    public sealed class TransformingGenerator<TIn, TOut> : IGenerator<TOut>
    {
        private readonly IGenerator<TIn> _source;
        private readonly Func<TIn, TOut> _transformer;

        public TransformingGenerator(IGenerator<TIn> source, Func<TIn, TOut> transformer)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _transformer = transformer ?? throw new ArgumentNullException(nameof(transformer));
        }

        public TOut Next()
            => _transformer.Invoke(_source.Next());
    }

    public class RandomStringGenerator : IGenerator<string>, IDisposable
    {
        public static readonly string DefaultAlphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-_";
        public static readonly string Base64Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ+/";
        public static readonly string Base32Alphabet = "0123456789abcdefghijklmnopqrstuv";
        public static readonly string Base16Alphabet = "0123456789abcdef";
        public static readonly RandomStringGenerator Default = new RandomStringGenerator();

        protected readonly RandomNumberGenerator Rng;
        protected object Lock => Rng;

        public string Alphabet { get; }
        public int Length { get; }

        public RandomStringGenerator(int length = 12, string? alphabet = null, RandomNumberGenerator? rng = null)
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

        public string Next() => Next(Length);
        public string Next(int length, string? alphabet = null)
        {
            if (alphabet == null)
                alphabet = Alphabet;
            else if (alphabet.Length < 1)
                throw new ArgumentOutOfRangeException(nameof(alphabet));
            var buffer = MemoryBuffer<byte>.LeaseAndSetCount(length);
            try {
                lock (Lock) {
                    Rng.GetBytes(buffer.Span);
                }
                return string.Create(length, (buffer.BufferMemory, alphabet), (charSpan, arg) => {
                    var (bufferMemory, alphabet1) = arg;
                    var bufferSpan = bufferMemory.Span;
                    var alphabetSpan = alphabet1.AsSpan();
                    var alphabetLength = alphabetSpan.Length;
                    if (Bits.IsPowerOf2((uint) alphabetLength)) {
                        var alphabetMask = alphabetLength - 1;
                        for (var i = 0; i < charSpan.Length; i++)
                            charSpan[i] = alphabetSpan[bufferSpan[i] & alphabetMask];
                    }
                    else {
                        for (var i = 0; i < charSpan.Length; i++)
                            charSpan[i] = alphabetSpan[bufferSpan[i] % alphabetLength];
                    }
                });
            }
            finally {
                buffer.Release();
            }
        }
    }

    public class RandomSymbolGenerator : RandomStringGenerator, IGenerator<Symbol>
    {
        public new static readonly RandomSymbolGenerator Default = new RandomSymbolGenerator();

        public string Prefix { get; }

        public RandomSymbolGenerator(string prefix = "", int length = 12, string? alphabet = null, RandomNumberGenerator? rng = null)
            : base(length, alphabet, rng)
            => Prefix = prefix;

        public new Symbol Next() => new Symbol(Prefix + base.Next());
    }
}
