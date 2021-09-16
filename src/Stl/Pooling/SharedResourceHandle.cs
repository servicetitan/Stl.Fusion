using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl.Pooling
{
    public readonly struct SharedResourceHandle<TKey, TResource> : IAsyncDisposable, IEquatable<SharedResourceHandle<TKey, TResource>>
    {
        public TKey Key { get; }
        public TResource Resource { get; }
        public bool IsValid => _releaser != null;
        private readonly Func<TKey, TResource, ValueTask>? _releaser;

        public SharedResourceHandle(TKey key, TResource resource, Func<TKey, TResource, ValueTask>? releaser)
        {
            Key = key;
            Resource = resource;
            _releaser = releaser;
        }

        public ValueTask DisposeAsync()
            => _releaser?.Invoke(Key, Resource) ?? ValueTaskExt.CompletedTask;

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
