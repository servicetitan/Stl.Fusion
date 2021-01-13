using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Stl.Fusion.Operations;
using Stl.Serialization;
using Stl.Time;

namespace Stl.Fusion.EntityFramework
{
    [Table("_Operations")]
    [Index(nameof(StartTime), Name = "IX_StartTime")]
    [Index(nameof(CommitTime), Name = "IX_CommitTime")]
    public class DbOperation : IOperation
    {
        private readonly JsonSerialized<object?> _command = new();
        private DateTime _startTime;
        private DateTime _commitTime;

        [Key]
        public string Id { get; set; } = "";
        public string AgentId { get; set; } = "";

        public DateTime StartTime {
            get => _startTime;
            set => _startTime = value.DefaultKind(DateTimeKind.Utc);
        }

        public DateTime CommitTime {
            get => _commitTime;
            set => _commitTime = value.DefaultKind(DateTimeKind.Utc);
        }

        public string CommandJson {
            get => _command.SerializedValue;
            set => _command.SerializedValue = value;
        }

        [NotMapped, JsonIgnore]
        public object? Command {
            get => _command.Value;
            set => _command.Value = value;
        }
    }
}
