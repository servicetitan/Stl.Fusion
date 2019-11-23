using System;

namespace Stl.ImmutableModel.Internal
{
    public static class Errors
    {
        public static Exception InvalidUpdateKeyMismatch() =>
            new ArgumentException("Invalid update: source.Key != target.Key.");

        public static Exception KeyIsUndefined() =>
            new InvalidOperationException("Key is undefined (likely, it wasn't ever set).");

        public static Exception PropertyNotFound(Type type, string propertyName) =>
            new InvalidOperationException(
                $"Type '{type.FullName}' doesn't have '{propertyName}' property.");

        public static Exception CannotCreateNodeTypeDef(Type type) =>
            new InvalidOperationException(
                $"Can't find '{nameof(SimpleNodeBase.CreateNodeTypeDef)} method for type '{type.FullName}'.");

        public static Exception InvalidOptionsKey() =>
            new ArgumentOutOfRangeException(
                $"Invalid option key. Valid key must start with '@' character.");

        public static Exception InvalidKeyFormat() =>
            new FormatException("Invalid key format.");

        public static Exception ContinuationCannotBeUndefinedKey(string paramName) =>
            new ArgumentOutOfRangeException(paramName, "Continuation cannot be UndefinedKey.");
    }
}
