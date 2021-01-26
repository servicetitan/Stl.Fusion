using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.EntityFramework.Authentication
{
    [Table("ExternalUsers")]
    [Index(nameof(Id))]
    public class DbExternalUserRef : IHasId<string>
    {
        [Key]
        public string Id { get; set; } = "";
        public long UserId { get; set; }
    }
}
