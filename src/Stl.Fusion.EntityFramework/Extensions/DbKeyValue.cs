using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.EntityFramework.Extensions;

[Table("_KeyValues")]
[Index(nameof(ExpiresAt))]
public class DbKeyValue
{
    private DateTime? _expiresAt;

    [Key] public string Key { get; set; } = "";
    public string Value { get; set; } = "";

    public DateTime? ExpiresAt {
        get => _expiresAt.DefaultKind(DateTimeKind.Utc);
        set => _expiresAt = value.DefaultKind(DateTimeKind.Utc);
    }
}
