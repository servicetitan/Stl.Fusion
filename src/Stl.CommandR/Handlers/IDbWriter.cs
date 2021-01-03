using Microsoft.EntityFrameworkCore;

namespace Stl.CommandR.Handlers
{
    public interface IDbWriter<TDbContext> : ICommand
        where TDbContext : DbContext
    {
    }
}
