using System;

namespace Stl.Fusion.Authentication
{
    public class SessionInfo : IHasId<string>
    {
        public string Id { get; set; } = "";
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public string IPAddress { get; set; } = "";
        public string Device { get; set; } = "";
        public string Location { get; set; } = "";

        public SessionInfo() { }
        public SessionInfo(string id) => Id = id;
    }
}
