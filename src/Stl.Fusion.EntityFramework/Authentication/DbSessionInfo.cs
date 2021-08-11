using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Stl.Collections;
using Stl.Conversion;
using Stl.Fusion.Authentication;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Serialization;
using Stl.Time;

namespace Stl.Fusion.EntityFramework.Authentication
{
    [Table("_Sessions")]
    [Index(nameof(CreatedAt), nameof(IsSignOutForced))]
    [Index(nameof(LastSeenAt), nameof(IsSignOutForced))]
    [Index(nameof(UserId), nameof(IsSignOutForced))]
    [Index(nameof(IPAddress), nameof(IsSignOutForced))]
    public class DbSessionInfo<TDbUserId> : IHasId<string>
        where TDbUserId : notnull
    {
        private readonly NewtonsoftJsonSerialized<ImmutableOptionSet?> _options = new(ImmutableOptionSet.Empty);
        private DateTime _createdAt;
        private DateTime _lastSeenAt;

        [Key, StringLength(32)]
        public string Id { get; set; } = "";
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
        public TDbUserId? UserId { get; set; }
        public bool IsSignOutForced { get; set; }

        // Options
        public string OptionsJson {
            get => _options.Data;
            set => _options.Data = value;
        }

        [NotMapped, JsonIgnore]
        public ImmutableOptionSet Options {
            get => _options.Value ?? ImmutableOptionSet.Empty;
            set => _options.Value = value;
        }

        public virtual SessionInfo ToModel(IServiceProvider services)
        {
            var dbUserIdHandler = services.GetRequiredService<IDbUserIdHandler<TDbUserId>>();
            var sessionInfo = new SessionInfo() {
                Id = Id,
                CreatedAt = CreatedAt,
                LastSeenAt = LastSeenAt,
                IPAddress = IPAddress,
                UserAgent = UserAgent,
                Options = Options,

                // Authentication
                AuthenticatedIdentity = AuthenticatedIdentity,
                UserId = UserId == null ? "" : dbUserIdHandler.Format(UserId),
                IsSignOutForced = IsSignOutForced,
            };
            return sessionInfo.OrDefault(Id); // To mask signed out sessions
        }

        public virtual void UpdateFrom(IServiceProvider services, SessionInfo source)
        {
            if (Id != source.Id)
                throw new ArgumentOutOfRangeException(nameof(source));
            if (IsSignOutForced)
                throw Errors.ForcedSignOut();

            var dbUserIdHandler = services.GetRequiredService<IDbUserIdHandler<TDbUserId>>();
            LastSeenAt = source.LastSeenAt;
            IPAddress = source.IPAddress;
            UserAgent = source.UserAgent;
            Options = source.Options;

            AuthenticatedIdentity = source.AuthenticatedIdentity;
            UserId = string.IsNullOrEmpty(source.UserId)
                ? default
                : dbUserIdHandler.Parse(source.UserId);
            IsSignOutForced = source.IsSignOutForced;
        }
    }
}
