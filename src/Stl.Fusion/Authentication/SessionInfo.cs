using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Stl.Fusion.Authentication
{
    public class SessionInfo : IHasId<string>
    {
        public string Id { get; init; } = "";
        public DateTime CreatedAt { get; init; }
        public DateTime LastSeenAt { get; init; }
        public string IPAddress { get; init; } = "";
        public string UserAgent { get; init; } = "";
        public IReadOnlyDictionary<string, object> ExtraProperties { get; init; } =
            ImmutableDictionary<string, object>.Empty;

        public SessionInfo() { }
        public SessionInfo(string id) => Id = id;

        public override string ToString() => $"{GetType()}({Id})";
    }
}
