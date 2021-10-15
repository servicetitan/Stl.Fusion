using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.Tests.Model;

[Index(nameof(Title))]
public record Chat : LongKeyedEntity
{
    [Required, MaxLength(120)]
    public string Title { get; init; } = "";
    [Required]
    public User Author { get; init; } = default!;
}
