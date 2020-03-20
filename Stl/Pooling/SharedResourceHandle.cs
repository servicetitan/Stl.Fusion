using System;
using System.Collections.Generic;

namespace Stl.Pooling
{
    public readonly struct SharedResourceHandle<TKey, TResource> : IDisposable, IEquatable<SharedResourceHandle<TKey, TResource>>
    {
        public TKey Key { get; }
        public TResource Resource { get; }
        public bool IsValid => _disposer != null;
        private readonly Action<TKey, TResource>? _disposer;
        
        public SharedResourceHandle(TKey key, TResource resource, Action<TKey, TResource>? disposer)
        {
            Key = key;
            Resource = resource;
            _disposer = disposer;
        }

        public void Dispose() => _disposer?.Invoke(Key, Resource);

        public void Deconstruct(out TKey key, out TResource resource)
        {
            key = Key;
            resource = Resource;
        }

        public override string ToString() => $"{GetType().Name}({Key}, {Resource})";

        // Equality

        public bool Equals(SharedResourceHandle<TKey, TResource> other) 
            => Equals(_disposer, other._disposer) 
                && EqualityComparer<TKey>.Default.Equals(Key, other.Key) 
                && EqualityComparer<TResource>.Default.Equals(Resource, other.Resource);

        public override bool Equals(object? obj) => obj is SharedResourceHandle<TKey, TResource> other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(_disposer, Key, Resource);
        public static bool operator ==(SharedResourceHandle<TKey, TResource> left, SharedResourceHandle<TKey, TResource> right) => left.Equals(right);
        public static bool operator !=(SharedResourceHandle<TKey, TResource> left, SharedResourceHandle<TKey, TResource> right) => !left.Equals(right);
    }
}
