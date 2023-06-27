using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.Tests.Model;

[Table("TestUsers")]
[Index(nameof(Name))]
[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial record User : LongKeyedEntity
{
    [Required, MaxLength(120)]
    [DataMember, MemoryPackOrder(1)]
    public string Name { get; init; } = "";

    [Required, MaxLength(250)]
    [DataMember, MemoryPackOrder(2)]
    public string Email { get; init; } = "";
}
