using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.Tests.Model
{
    [Table("TestUsers")]
    [Index(nameof(Name))]
    public record User : LongKeyedEntity
    {
        [Required, MaxLength(120)]
        public string Name { get; init; } = "";
        [Required, MaxLength(250)]
        public string Email { get; init; } = "";
    }
}
