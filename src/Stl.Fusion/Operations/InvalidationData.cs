using Stl.CommandR;

namespace Stl.Fusion.Operations
{
    // A helper Box<T> - like type allowing command handlers to store
    // invalidation data inside CommandContext.Items; such data
    // is supposed to be persisted to the database & restored
    // on invalidation pass
    public interface IInvalidationData
    { }

    public record InvalidationData<T>(T Value) : IInvalidationData
    { }

    public record InvalidationData<TScope, T>(T Value) : IInvalidationData
    { }

    public static class InvalidationData
    {
        public static InvalidationData<T> New<T>(T value) => new(value);
        public static InvalidationData<TScope, T> New<TScope, T>(T value) => new(value);
    }
}
