using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Stl.Collections;
using Stl.Fusion.Authentication;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Serialization;
using Stl.Time;

namespace Stl.Fusion.EntityFramework.Authentication
{
    [Table("Sessions")]
    [Index(nameof(CreatedAt), nameof(IsSignOutForced))]
    [Index(nameof(LastSeenAt), nameof(IsSignOutForced))]
    [Index(nameof(UserId), nameof(IsSignOutForced))]
    [Index(nameof(IPAddress), nameof(IsSignOutForced))]
    public class DbSessionInfo : IHasId<string>
    {
        private readonly JsonSerialized<ImmutableOptionSet?> _options = new(ImmutableOptionSet.Empty);
        private DateTime _createdAt;
        private DateTime _lastSeenAt;

        [Key] public string Id { get; set; } = "";
        public DateTime CreatedAt {
            get => _createdAt.DefaultKind(DateTimeKind.Utc);
            set => _createdAt = value.DefaultKind(DateTimeKind.Utc);
        }
        public DateTime LastSeenAt {
            get => _lastSeenAt.DefaultKind(DateTimeKind.Utc);
            set => _lastSeenAt = value.DefaultKind(DateTimeKind.Utc);
        }
        public string IPAddress { get; set; } = "";
        public string UserAgent { get; set; } = "";

        // Authentication
        public string AuthenticatedIdentity { get; set; } = "";
        public long? UserId { get; set; }
        public bool IsSignOutForced { get; set; }

        // Options
        public string OptionsJson {
            get => _options.SerializedValue;
            set => _options.SerializedValue = value;
        }

        [NotMapped, JsonIgnore]
        public ImmutableOptionSet Options {
            get => _options.Value ?? ImmutableOptionSet.Empty;
            set => _options.Value = value;
        }

        public virtual SessionInfo ToModel()
        {
            var sessionInfo = new SessionInfo() {
                Id = Id,
                CreatedAt = CreatedAt,
                LastSeenAt = LastSeenAt,
                IPAddress = IPAddress,
                UserAgent = UserAgent,
                Options = Options,

                // Authentication
                AuthenticatedIdentity = AuthenticatedIdentity,
                UserId = UserId?.ToString() ?? "",
                IsSignOutForced = IsSignOutForced,
            };
            return sessionInfo.OrDefault(Id); // To mask signed out sessions
        }

        public virtual void FromModel(SessionInfo source)
        {
            if (Id != source.Id)
                throw new ArgumentOutOfRangeException(nameof(source));
            if (IsSignOutForced)
                throw Errors.ForcedSignOut();

            LastSeenAt = source.LastSeenAt;
            IPAddress = source.IPAddress;
            UserAgent = source.UserAgent;
            Options = source.Options;

            AuthenticatedIdentity = source.AuthenticatedIdentity;
            UserId = string.IsNullOrEmpty(source.UserId)
                ? null
                : long.Parse(source.UserId);
            IsSignOutForced = source.IsSignOutForced;
        }
    }
}
