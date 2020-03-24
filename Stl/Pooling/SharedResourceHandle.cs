using System;
using System.Collections.Generic;

namespace Stl.Pooling
{
    public readonly struct SharedResourceHandle<TKey, TResource> : IDisposable, IEquatable<SharedResourceHandle<TKey, TResource>>
    {
        public TKey Key { get; }
        public TResource Resource { get; }
        public bool IsValid => _releaser != null;
        private readonly Action<TKey, TResource>? _releaser;
        
        public SharedResourceHandle(TKey key, TResource resource, Action<TKey, TResource>? releaser)
        {
            Key = key;
            Resource = resource;
            _releaser = releaser;
        }

        public void Dispose() => _releaser?.Invoke(Key, Resource);

        public void Deconstruct(out TKey key, out TResource resource)
        {
            key = Key;
            resource = Resource;
        }

        public override string ToString() => $"{GetType().Name}({Key}, {Resource})";

        // Equality

        public bool Equals(SharedResourceHandle<TKey, TResource> other) 
            => Equals(_releaser, other._releaser) 
                && EqualityComparer<TKey>.Default.Equals(Key, other.Key) 
                && EqualityComparer<TResource>.Default.Equals(Resource, other.Resource);

        public override bool Equals(object? obj) => obj is SharedResourceHandle<TKey, TResource> other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(_releaser, Key, Resource);
        public static bool operator ==(SharedResourceHandle<TKey, TResource> left, SharedResourceHandle<TKey, TResource> right) => left.Equals(right);
        public static bool operator !=(SharedResourceHandle<TKey, TResource> left, SharedResourceHandle<TKey, TResource> right) => !left.Equals(right);
    }
}
