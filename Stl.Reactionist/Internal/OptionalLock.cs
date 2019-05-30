using System;
using System.Threading;

namespace Reactionist.Internal
{
    public struct OptionalLock : IEquatable<OptionalLock>
    {
        public struct OptionalLockReleaser : IDisposable
        {
            private readonly object _lock;

            public OptionalLockReleaser(object @lock)
            {
                _lock = @lock;
            }

            public void Dispose()
            {
                if (_lock != null)
                    Monitor.Exit(_lock);
            }
        }

        private readonly object _lock;
        public bool IsLocking => _lock != null;
        
        public OptionalLock(object @lock)
        {
            _lock = @lock;
        }

        public OptionalLockReleaser Acquire()
        {
            if (_lock != null)
                Monitor.Enter(_lock);
            return new OptionalLockReleaser(_lock);
        }
        
        // Equality

        public bool Equals(OptionalLock other) => _lock == other._lock;
        public override bool Equals(object obj) => 
            !ReferenceEquals(null, obj) && (obj is OptionalLock other && Equals(other));
        public override int GetHashCode() => _lock?.GetHashCode() ?? 0;
        public static bool operator ==(OptionalLock left, OptionalLock right) => left.Equals(right);
        public static bool operator !=(OptionalLock left, OptionalLock right) => !left.Equals(right);
    }
}
