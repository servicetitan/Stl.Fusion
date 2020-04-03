using System.ComponentModel.DataAnnotations;

namespace Stl.Tests.Purifier.Model
{
    public class Post : LongKeyedEntity
    {
        private string _title = "";
        private string _text = "";
        private User _author = default!;

        [Required, MaxLength(120)]
        public string Title {
            get => _title;
            set => _title = PreparePropertyValue(nameof(Title), value);
        }

        [Required, MaxLength(1_000_000)]
        public string Text {
            get => _text;
            set => _text = PreparePropertyValue(nameof(Text), value);
        }

        public User Author {
            get => _author;
            set => _author = PreparePropertyValue(nameof(Author), value);
        }
    }
}
