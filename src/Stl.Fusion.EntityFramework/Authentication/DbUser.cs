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
        public string AuthenticationType { get; set; } = "";
        public string Name { get; set; } = "";
        public string ClaimsJson { get; set; } = "";
    }

    [Table("ExternalUsers")]
    [Index(nameof(ExternalId))]
    public class DbExternalUser : IHasId<string>
    {
        [Key]
        public string ExternalId { get; set; } = "";
        public long UserId { get; set; }

        [NotMapped]
        string IHasId<string>.Id => ExternalId;
    }
}
