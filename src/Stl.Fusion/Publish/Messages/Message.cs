using System;
using Stl.Text;

namespace Stl.Fusion.Publish.Messages
{
    [Serializable]
    public abstract class Message
    {
        Symbol? Id { get; set; }
    }
}
