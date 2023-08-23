using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Stl.Fusion.EntityFramework.Internal;

public static class DatabaseFacadeExt
{
    public static bool IsInMemory(this DatabaseFacade database)
        => database.ProviderName?.EndsWith(".InMemory", StringComparison.Ordinal) ?? false;

    public static void DisableAutoTransactionsAndSavepoints(this DatabaseFacade database)
    {
#if NET7_0_OR_GREATER
        database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
#else
        database.AutoTransactionsEnabled = false;
#endif
#if NET6_0_OR_GREATER
        database.AutoSavepointsEnabled = false;
#endif
    }
}
