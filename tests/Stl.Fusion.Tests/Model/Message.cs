using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.Tests.Model
{
    [Index(nameof(Date))]
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
