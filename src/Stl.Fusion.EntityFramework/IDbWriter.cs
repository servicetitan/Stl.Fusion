using Microsoft.EntityFrameworkCore;
using Stl.CommandR;

namespace Stl.Fusion.EntityFramework
{
    public interface IDbWriter<TDbContext> : ICommand
        where TDbContext : DbContext
    { }
}
