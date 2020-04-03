using Stl.ImmutableModel;

namespace Stl.Tests.Purifier
{
    public class User : Node<LongKey>
    {
        private string _name = "";
        private string _email = "";

        public string Name {
            get => _name;
            set { this.ThrowIfFrozen(); _name = value; }
        }

        public string Email {
            get => _email;
            set { this.ThrowIfFrozen(); _email = value; }
        }
    }
}
