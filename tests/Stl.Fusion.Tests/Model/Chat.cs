using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.Tests.Model;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[Index(nameof(Title))]
public partial record Chat : LongKeyedEntity
{
    [Required, MaxLength(120)]
    [DataMember, MemoryPackOrder(1)]
    public string Title { get; init; } = "";

    [Required]
    [DataMember, MemoryPackOrder(2)]
    public User Author { get; init; } = default!;
}
