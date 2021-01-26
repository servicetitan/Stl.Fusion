using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.EntityFramework.Authentication
{
    [Table("Sessions")]
    [Index(nameof(CreatedAt))]
    [Index(nameof(LastSeenAt))]
    [Index(nameof(UserId))]
    [Index(nameof(IPAddress))]
    public class DbSession : IHasId<string>
    {
        [Key]
        public string Id { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime LastSeenAt { get; set; }
        public string IPAddress { get; set; } = "";
        public string UserAgent { get; set; } = "";
        public string ExtraPropertiesJson { get; set; } = "";
        public long? UserId { get; set; }
        public bool IsSignOutForced { get; set; } = false;
    }
}
