using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

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
        get => _command.Data;
        set => _command.Data = value;
    }

    public string ItemsJson {
        get => _items.Data;
        set => _items.Data = value;
    }

    [NotMapped, JsonIgnore]
    public object? Command {
        get => _command.Value;
        set => _command.Value = value;
    }

    [NotMapped, JsonIgnore]
    public OptionSet Items {
        get => _items.Value;
        set => _items.Value = value;
    }
}
