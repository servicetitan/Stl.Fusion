using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Stl.Tests.Purifier.Model
{
    public class User : LongKeyedEntity
    {
        private string _name = "";
        private string _email = "";
        private PostCollection _posts = new PostCollection();

        [Required, MaxLength(120)]
        public string Name {
            get => _name;
            set => _name = PreparePropertyValue(nameof(Name), value);
        }

        [Required, MaxLength(250)]
        public string Email {
            get => _email;
            set => _name = PreparePropertyValue(nameof(Email), value);
        }

        [NotMapped]
        public PostCollection Posts {
            get => _posts;
            set => _posts = PreparePropertyValue(nameof(Posts), value);
        }
    }
}
