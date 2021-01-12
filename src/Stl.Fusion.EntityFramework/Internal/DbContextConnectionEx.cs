using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Fusion.EntityFramework.Internal
{
    public static class DbContextConnectionEx
    {
        private static readonly FieldInfo LeaseField = typeof(DbContext)
            .GetField("_lease", BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly FieldInfo ConnectionField = typeof(RelationalConnection)
            .GetField("_connection", BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly FieldInfo ConnectionOwnedField = typeof(RelationalConnection)
            .GetField("_connectionOwned", BindingFlags.Instance | BindingFlags.NonPublic)!;

        public static DbConnection? GetDbConnection(this DbContext dbContext)
        {
            var relationalConnection = (RelationalConnection) dbContext.Database
                .GetInfrastructure()
                .GetRequiredService<IRelationalConnection>();
            return (DbConnection?) ConnectionField.GetValue(relationalConnection);
        }

        public static void SetDbConnection(this DbContext dbContext,
            DbConnection? dbConnection, bool isOwned = false)
        {
            var relationalConnection = (RelationalConnection) dbContext.Database
                .GetInfrastructure()
                .GetRequiredService<IRelationalConnection>();
            var oldDbConnection = (DbConnection?) ConnectionField.GetValue(relationalConnection);
            var oldIsOwned = (bool) ConnectionOwnedField.GetValue(relationalConnection)!;
            ConnectionField.SetValue(relationalConnection, dbConnection);
            ConnectionOwnedField.SetValue(relationalConnection, isOwned);
#pragma warning disable EF1001
            LeaseField.SetValue(dbContext, DbContextLease.InactiveLease);
#pragma warning restore
        }
    }
}
