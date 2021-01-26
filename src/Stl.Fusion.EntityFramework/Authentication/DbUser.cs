using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.EntityFramework.Authentication
{
    [Table("Users")]
    [Index(nameof(Name))]
    public class DbUser : IHasId<long>
    {
        [Key]
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public string ClaimsJson { get; set; } = "";
    }
}
