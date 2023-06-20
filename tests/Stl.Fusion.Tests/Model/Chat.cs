using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.Tests.Model;

[DataContract, MemoryPackable]
[Index(nameof(Title))]
public partial record Chat : LongKeyedEntity
{
    [DataMember, Required, MaxLength(120)]
    public string Title { get; init; } = "";
    [DataMember, Required]
    public User Author { get; init; } = default!;
}
