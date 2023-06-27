using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.Tests.Model;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[Index(nameof(Date))]
public partial record Message : LongKeyedEntity
{
    [DataMember, MemoryPackOrder(1)]
    public DateTime Date { get; init; }

    [Required, MaxLength(1_000_000)]
    [DataMember, MemoryPackOrder(2)]
    public string Text { get; init; } = "";

    [Required]
    [DataMember, MemoryPackOrder(3)]
    public User Author { get; init; } = default!;

    [Required]
    [DataMember, MemoryPackOrder(4)]
    public Chat Chat { get; init; } = default!;
}
