namespace Stl.Fusion.Operations
{
    // A helper Box<T> - like type allowing command handlers to store
    // data related to operation inside CommandContext.Items.
    // Such items are persisted to the operation log & restored
    // on invalidation replay pass.
    public interface IOperationItem
    { }

    public record OperationItem<T>(T Value) : IOperationItem
    { }

    public record OperationItem<TScope, T>(T Value) : IOperationItem
    { }

    public static class OperationItem
    {
        public static OperationItem<T> New<T>(T value) => new(value);
        public static OperationItem<TScope, T> New<TScope, T>(T value) => new(value);
    }
}
