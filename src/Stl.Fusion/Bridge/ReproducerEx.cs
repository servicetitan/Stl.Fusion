using System.Collections.Generic;
using Stl.Text;

namespace Stl.Fusion.Bridge
{
    public static class ReproducerEx
    {
        public static IReproduction Get(this IReproducer reproducer, Symbol publicationId) 
            => reproducer.TryGet(publicationId) ?? throw new KeyNotFoundException();

        public static IReproduction<T>? TryGet<T>(this IReproducer reproducer, Symbol publicationId)
            => reproducer.TryGet(publicationId) as IReproduction<T>;
        public static IReproduction<T> Get<T>(this IReproducer reproducer, Symbol publicationId)
            => (IReproduction<T>) reproducer.Get(publicationId);
    }
}
