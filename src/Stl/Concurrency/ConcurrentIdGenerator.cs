using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Stl.Mathematics;
using Stl.OS;
using Stl.Security;

namespace Stl.Concurrency
{
    public sealed class ConcurrentIdGenerator<T> : IGenerator<T>
    {
        private readonly Func<T>[] _generators;

        public int ConcurrencyLevel => _generators.Length;
        public int ConcurrencyLevelMask { get; }

        public ConcurrentIdGenerator(Func<int, Func<T>> generatorFactory)
            : this(generatorFactory, ConcurrentIdGenerator.DefaultConcurrencyLevel) { }
        public ConcurrentIdGenerator(Func<int, Func<T>> generatorFactory, int concurrencyLevel)
            : this(Enumerable.Range(0, concurrencyLevel).Select(generatorFactory)) { }
        public ConcurrentIdGenerator(IEnumerable<Func<T>> generators)
            : this(generators.ToArray()) { }
        public ConcurrentIdGenerator(Func<T>[] generators)
        {
            if (!Bits.IsPowerOf2((uint) generators.Length))
                throw new ArgumentOutOfRangeException(nameof(generators));
            _generators = generators;
            ConcurrencyLevelMask = generators.Length - 1;
        }

        public T Next() => Next(Thread.CurrentThread.ManagedThreadId);

        public T Next(int random)
        {
            var generator = _generators[random & ConcurrencyLevelMask];
            lock (generator)
                return generator.Invoke();
        }
    }

    public static class ConcurrentIdGenerator
    {
        public static int DefaultConcurrencyLevel => HardwareInfo.ProcessorCountPo2 << 1;
        public static readonly ConcurrentIdGenerator<int> DefaultInt32 = NewInt32();
        public static readonly ConcurrentIdGenerator<long> DefaultInt64 = NewInt64();
        public static readonly ConcurrentIdGenerator<LTag> DefaultLTag = NewLTag();

        public static ConcurrentIdGenerator<int> NewInt32(int concurrencyLevel = -1)
        {
            if (concurrencyLevel < 0)
                concurrencyLevel = DefaultConcurrencyLevel;
            concurrencyLevel = (int) Bits.GreaterOrEqualPowerOf2((uint) concurrencyLevel);
            return new ConcurrentIdGenerator<int>(i => {
                var count = (uint) 0;
                return () => {
                    unchecked {
                        count += (uint) concurrencyLevel;
                        return (int) (count ^ i);
                    }
                };
            });
        }

        public static ConcurrentIdGenerator<long> NewInt64(int concurrencyLevel = -1)
        {
            if (concurrencyLevel < 0)
                concurrencyLevel = DefaultConcurrencyLevel;
            concurrencyLevel = (int) Bits.GreaterOrEqualPowerOf2((uint) concurrencyLevel);
            return new ConcurrentIdGenerator<long>(i => {
                var count = (ulong) 0;
                return () => {
                    unchecked {
                        count += (ulong) concurrencyLevel;
                        return (long) (count ^ (ulong) i);
                    }
                };
            });
        }

        public static ConcurrentIdGenerator<LTag> NewLTag(int concurrencyLevel = -1)
        {
            if (concurrencyLevel < 0)
                concurrencyLevel = DefaultConcurrencyLevel;
            concurrencyLevel = (int) Bits.GreaterOrEqualPowerOf2((uint) concurrencyLevel);
            var maxCount = long.MaxValue >> 8; // Let's have some reserve @ the top of the band
            return new ConcurrentIdGenerator<LTag>(i => {
                var count = (long) 0;
                return () => {
                    unchecked {
                        count = (count + concurrencyLevel) & maxCount;
                        // We want to return only strictly positive LTags (w/o IsSpecial flag)
                        if (count == 0)
                            count = 1;
                        return new LTag(count);
                    }
                };
            });
        }
    }
}
