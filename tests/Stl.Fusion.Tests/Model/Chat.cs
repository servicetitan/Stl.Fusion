using System.ComponentModel.DataAnnotations;

namespace Stl.Fusion.Tests.Model
{
    public record Chat : LongKeyedEntity
    {
        [Required, MaxLength(120)]
        public string Title { get; init; } = "";
        [Required]
        public User Author { get; init; } = default!;
    }
}
