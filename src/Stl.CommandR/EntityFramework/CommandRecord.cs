using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Stl.Serialization;

namespace Stl.CommandR.EntityFramework
{
    [Table("Commands")]
    [Index(nameof(StartTime), Name = "IX_StartTime")]
    public class CommandRecord
    {
        private readonly JsonSerialized<ICommand> _command = new();

        [Key]
        public string Id { get; set; } = "";
        public DateTime StartTime { get; set; }

        public string CommandJson {
            get => _command.SerializedValue;
            set => _command.SerializedValue = value;
        }

        [JsonIgnore]
        [NotMapped]
        public ICommand Command {
            get => _command.Value;
            set => _command.Value = value;
        }

        public CommandRecord() { }
        public CommandRecord(DateTime startTime, ICommand command)
            : this(Ulid.NewUlid().ToString(), startTime, command) { }
        public CommandRecord(ICommand command)
            : this(DateTime.UtcNow, command) { }
        public CommandRecord(string id, DateTime startTime, ICommand command)
        {
            Id = id;
            StartTime = startTime;
            Command = command;
        }
    }
}
