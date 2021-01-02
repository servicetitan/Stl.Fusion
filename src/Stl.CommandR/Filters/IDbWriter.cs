using Microsoft.EntityFrameworkCore;

namespace Stl.CommandR.Filters
{
    public interface IDbWriter<TDbContext> : ICommand
        where TDbContext : DbContext
    {
    }
}
