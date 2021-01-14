using System.ComponentModel.DataAnnotations;

namespace Stl.Tests.CommandR.Services
{
    public class User
    {
        [Key]
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
    }
}
