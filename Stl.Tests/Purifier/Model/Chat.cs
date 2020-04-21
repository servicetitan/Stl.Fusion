using System.ComponentModel.DataAnnotations;

namespace Stl.Tests.Purifier.Model
{
    public class Chat : LongKeyedEntity
    {
        private string _title = "";
        private User _author = default!;

        [Required, MaxLength(120)]
        public string Title {
            get => _title;
            set => _title = PreparePropertyValue(nameof(Title), value);
        }

        [Required]
        public User Author {
            get => _author;
            set => _author = PreparePropertyValue(nameof(Author), value);
        }
    }
}
