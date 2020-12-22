using System.ComponentModel.DataAnnotations;

namespace Stl.Fusion.Tests.Model
{
    public record User : LongKeyedEntity
    {
        [Required, MaxLength(120)]
        public string Name { get; init; } = "";
        [Required, MaxLength(250)]
        public string Email { get; init; } = "";
    }
}
