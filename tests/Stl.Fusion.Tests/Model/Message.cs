using System;
using System.ComponentModel.DataAnnotations;

namespace Stl.Fusion.Tests.Model
{
    public record Message : LongKeyedEntity
    {
        public DateTime Date { get; init; }
        [Required, MaxLength(1_000_000)]
        public string Text { get; init; } = "";
        [Required]
        public User Author { get; init; } = default!;
        [Required]
        public Chat Chat { get; init; } = default!;
    }
}
