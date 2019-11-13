using System;

namespace Stl.ImmutableModel.Internal
{
    public static class Errors
    {
        public static Exception InvalidUpdateKeyMismatch() =>
            new ArgumentException("Invalid update: source.Key != target.Key.");

        public static Exception PropertyNotFound(Type type, string propertyName) =>
            new InvalidOperationException(
                $"Type '{type.FullName}' doesn't have '{propertyName}' property.");

        public static Exception CannotCreateNodeTypeInfo(Type type) =>
            new InvalidOperationException(
                $"Can't find '{nameof(SimpleNodeBase.CreateNodeTypeInfo)} method for type '{type.FullName}'.");
    }
}
