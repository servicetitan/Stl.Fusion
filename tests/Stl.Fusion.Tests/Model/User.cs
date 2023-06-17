using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.Tests.Model;

[Table("TestUsers")]
[Index(nameof(Name))]
[DataContract, MemoryPackable]
public partial record User : LongKeyedEntity
{
    [DataMember, Required, MaxLength(120)]
    public string Name { get; init; } = "";
    [DataMember, Required, MaxLength(250)]
    public string Email { get; init; } = "";
}
