using System;
using System.Collections.Generic;

namespace Stl.Fusion.Authentication
{
    public class SessionInfo : IHasId<string>
    {
        public string Id { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime LastSeenAt { get; set; }
        public string IPAddress { get; set; } = "";
        public string UserAgent { get; set; } = "";
        public Dictionary<string, object>? ExtraProperties { get; } = null;

        public SessionInfo() { }
        public SessionInfo(string id) => Id = id;
    }
}
