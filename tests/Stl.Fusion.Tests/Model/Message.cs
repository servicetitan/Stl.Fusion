using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.Tests.Model;

[DataContract, MemoryPackable]
[Index(nameof(Date))]
public partial record Message : LongKeyedEntity
{
    [DataMember]
    public DateTime Date { get; init; }
    [DataMember, Required, MaxLength(1_000_000)]
    public string Text { get; init; } = "";
    [DataMember, Required]
    public User Author { get; init; } = default!;
    [DataMember, Required]
    public Chat Chat { get; init; } = default!;
}
