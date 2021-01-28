using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.EntityFramework.Authentication
{
    [Table("UserIdentities")]
    [Index(nameof(Id))]
    public class DbUserIdentity : IHasId<string>
    {
        [Key] public string Id { get; set; } = "";
        public long UserId { get; set; }
        public string Secret { get; set; } = "";

        public virtual UserIdentity ToModel() => new(Id);
    }
}
