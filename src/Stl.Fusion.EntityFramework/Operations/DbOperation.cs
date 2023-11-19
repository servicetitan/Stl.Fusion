using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.EntityFramework.Operations;

[Table("_Operations")]
[Index(nameof(StartTime), Name = "IX_StartTime")]
[Index(nameof(CommitTime), Name = "IX_CommitTime")]
public class DbOperation : IOperation
{
    private readonly NewtonsoftJsonSerialized<object?> _command = NewtonsoftJsonSerialized.New(default(object?));
    private readonly NewtonsoftJsonSerialized<OptionSet> _items = NewtonsoftJsonSerialized.New(new OptionSet());
    private DateTime _startTime;
    private DateTime _commitTime;

    [Key] public string Id { get; set; } = "";
    public string AgentId { get; set; } = "";

    public DateTime StartTime {
        get => _startTime.DefaultKind(DateTimeKind.Utc);
        set => _startTime = value.DefaultKind(DateTimeKind.Utc);
    }

    public DateTime CommitTime {
        get => _commitTime.DefaultKind(DateTimeKind.Utc);
        set => _commitTime = value.DefaultKind(DateTimeKind.Utc);
    }

    public string CommandJson {
#pragma warning disable IL2026
        get => _command.Data;
#pragma warning restore IL2026
        set => _command.Data = value;
    }

    public string ItemsJson {
#pragma warning disable IL2026
        get => _items.Data;
#pragma warning restore IL2026
        set => _items.Data = value;
    }

    [NotMapped]
    public object? Command {
#pragma warning disable IL2026
        get => _command.Value;
#pragma warning restore IL2026
        set => _command.Value = value;
    }

    [NotMapped]
    public OptionSet Items {
#pragma warning disable IL2026
        get => _items.Value;
#pragma warning restore IL2026
        set => _items.Value = value;
    }
}
