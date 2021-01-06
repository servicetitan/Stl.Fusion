using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Stl.Serialization;

namespace Stl.EntityFramework
{
    public interface IDbOperation
    {
        string Id { get; set; }
        DateTime StartTime { get; set; }
        string AgentId { get; set; }
        object Operation { get; set; }
    }

    [Table("_Operations")]
    [Index(nameof(StartTime), Name = "IX_StartTime")]
    public class DbOperation : IDbOperation
    {
        private readonly JsonSerialized<object> _operation = new();

        [Key]
        public string Id { get; set; } = "";
        public DateTime StartTime { get; set; }
        public string AgentId { get; set; } = "";

        public string OperationJson {
            get => _operation.SerializedValue;
            set => _operation.SerializedValue = value;
        }

        [JsonIgnore]
        [NotMapped]
        public object Operation {
            get => _operation.Value;
            set => _operation.Value = value;
        }
    }
}
