using System;

namespace Stl.Fusion.Client.Internal
{
    public static class Errors
    {
        public static Exception InvalidUri(string uri)
            => new InvalidOperationException($"Invalid URI: '{uri}'.");

        public static Exception InterfaceTypeExpected(Type type, bool mustBePublic, string? argumentName = null)
        {
            var message = $"'{type}' must be {(mustBePublic ? "a public": "an")} interface type.";
            return string.IsNullOrEmpty(argumentName)
                ? (Exception) new InvalidOperationException(message)
                : new ArgumentOutOfRangeException(argumentName, message);
        }

        public static Exception CouldNotDeserializeServerSideError()
            => new ServiceException("Couldn't deserialize server-side error.");

        public static Exception UnknownServerSideError()
            => new ServiceException("Unknown server-side error.");
    }
}
