using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Stl.Collections;
using Stl.Serialization;
using Stl.Time;
using Stl.Versioning;

namespace Stl.Fusion.EntityFramework.Authentication;

[Table("_Sessions")]
[Index(nameof(CreatedAt), nameof(IsSignOutForced))]
[Index(nameof(LastSeenAt), nameof(IsSignOutForced))]
[Index(nameof(UserId), nameof(IsSignOutForced))]
[Index(nameof(IPAddress), nameof(IsSignOutForced))]
public class DbSessionInfo<TDbUserId> : IHasId<string>, IHasVersion<long>
{
    private readonly NewtonsoftJsonSerialized<ImmutableOptionSet> _options =
        NewtonsoftJsonSerialized.New(ImmutableOptionSet.Empty);
    private DateTime _createdAt;
    private DateTime _lastSeenAt;

    [Key, StringLength(32)]
    public string Id { get; set; } = "";
    [ConcurrencyCheck] public long Version { get; set; }

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
    public TDbUserId UserId { get; set; } = default!;
    public bool IsSignOutForced { get; set; }

    // Options
    public string OptionsJson {
        get => _options.Data;
        set => _options.Data = value;
    }

    [NotMapped, JsonIgnore]
    public ImmutableOptionSet Options {
        get => _options.Value;
        set => _options.Value = value;
    }
}
