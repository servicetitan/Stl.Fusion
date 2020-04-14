using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Stl.OS;

namespace Stl.Concurrency
{
    public sealed class ShortTermResourceRenter<T>
    {
        public static int DefaultConcurrencyLevel => HardwareInfo.ProcessorCount;

        public readonly struct Lease : IDisposable
        {
            public readonly ShortTermResourceRenter<T> Renter;
            public readonly int ResourceIndex;
            public T Resource => Renter._resources[ResourceIndex];
            public ref T ResourceRef => ref Renter._resources[ResourceIndex];

            internal Lease(ShortTermResourceRenter<T> renter, int resourceIndex)
            {
                Renter = renter;
                ResourceIndex = resourceIndex;
            }

            public void Dispose()
            {
                if (Renter != null)
                    Monitor.Exit(Renter._locks[ResourceIndex]);
            }
        }

        private readonly object[] _locks; 
        private readonly T[] _resources; 

        public int ConcurrencyLevel => _locks.Length;

        public ShortTermResourceRenter(Func<int, T> resourceFactory)
            : this(resourceFactory, DefaultConcurrencyLevel) { }
        public ShortTermResourceRenter(Func<int, T> resourceFactory, int concurrencyLevel)
            : this(Enumerable.Range(0, concurrencyLevel).Select(resourceFactory)) { }
        public ShortTermResourceRenter(IEnumerable<T> resources)
            : this(resources.ToArray()) { }
        public ShortTermResourceRenter(T[] resources)
        {
            if (resources.Length <= 0)
                throw new ArgumentOutOfRangeException(nameof(resources));
            _resources = resources;
            _locks = new object[resources.Length];
            for (var i = 0; i < _locks.Length; i++)
                _locks[i] = new object();
        }

        public Lease Rent(int workerId)
        {
            var index = (workerId & int.MaxValue) % ConcurrencyLevel;
            var @lock = _locks[index];
            Monitor.Enter(@lock);
            return new Lease(this, index);
        }
    }
}
