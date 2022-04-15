#if NETSTANDARD2_0

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

internal static class RelationalDatabaseFacadeExtensions
{
    private static readonly FieldInfo ConnectionField = typeof(RelationalConnection)
        .GetField("_connection", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo ConnectionOwnedField = typeof(RelationalConnection)
        .GetField("_connectionOwned", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public static void SetDbConnection(this DatabaseFacade database,
        DbConnection? dbConnection, bool isOwned = false)
    {
        var relationalConnection = (RelationalConnection) database
            .GetInfrastructure()
            .GetRequiredService<IRelationalConnection>();
        var oldDbConnection = (DbConnection?) ConnectionField.GetValue(relationalConnection);
        var oldIsOwned = (bool) ConnectionOwnedField.GetValue(relationalConnection)!;
        if (oldIsOwned)
            oldDbConnection?.Dispose();

        ConnectionField.SetValue(relationalConnection, dbConnection);
        ConnectionOwnedField.SetValue(relationalConnection, isOwned);
    }
}

#endif
