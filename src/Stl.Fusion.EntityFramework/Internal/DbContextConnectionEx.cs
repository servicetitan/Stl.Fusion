#if NETSTANDARD2_0

using System;
using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Stl.Async;

namespace Stl.Fusion.EntityFramework.Internal
{
    public static class DbContextConnectionEx
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
}

#endif