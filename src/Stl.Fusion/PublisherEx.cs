using System.Collections.Generic;
using Stl.Text;

namespace Stl.Fusion
{
    public static class PublisherEx
    {
        public static IPublication Get(this IPublisher publisher, Symbol publicationId) 
            => publisher.TryGet(publicationId) ?? throw new KeyNotFoundException();
    }
}
