using System;
using System.Collections.Generic;

namespace Stl.Pooling
{
    public readonly struct ResourceLease<T> : IDisposable, IEquatable<ResourceLease<T>>
    {
        private readonly IResourceReleaser<T> _releaser;
        public T Resource { get; }

        public ResourceLease(T resource, IResourceReleaser<T> releaser)
        {
            Resource = resource;
            _releaser = releaser;
        }

        public void Dispose()
        {
            if (_releaser?.Release(Resource) ?? false)
                return;
            if (Resource is IDisposable d)
                d.Dispose();
        }

        public override string ToString() => $"{GetType().Name}({Resource})";

        // Equality

        public bool Equals(ResourceLease<T> other)
            => ReferenceEquals(_releaser, other._releaser)
                && EqualityComparer<T>.Default.Equals(Resource, other.Resource);
        public override bool Equals(object? obj) => obj is ResourceLease<T> other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Resource, _releaser);
        public static bool operator ==(ResourceLease<T> left, ResourceLease<T> right) => left.Equals(right);
        public static bool operator !=(ResourceLease<T> left, ResourceLease<T> right) => !left.Equals(right);
    }
}
