using Cysharp.Text;
using Stl.Fusion.EntityFramework.Internal;

namespace Stl.Fusion.EntityFramework.Npgsql.Internal;

public class NpgsqlDbHintFormatter : DbHintFormatter
{
    public NpgsqlDbHintFormatter()
    {
        DbHintToSql = new Dictionary<DbHint, string>() {
            {DbLockingHint.KeyShare, "KEY SHARE"},
            {DbLockingHint.Share, "SHARE"},
            {DbLockingHint.NoKeyUpdate, "NO KEY UPDATE"},
            {DbLockingHint.Update, "UPDATE"},
            {DbWaitHint.NoWait, "NOWAIT"},
            {DbWaitHint.SkipLocked, "SKIP LOCKED"},
        };
    }

    public override string FormatSelectSql(string tableName, ref MemoryBuffer<DbHint> hints)
    {
        var sb = ZString.CreateStringBuilder();
        try {
            sb.Append("SELECT * FROM ");
            FormatTableNameTo(ref sb, tableName);
            var isFirst = true;
            foreach (var hint in hints) {
                if (isFirst)
                    sb.Append(" FOR ");
                else
                    sb.Append(' ');
                sb.Append(FormatHint(hint));
                isFirst = false;
            }
            return sb.ToString();
        }
        finally {
            sb.Dispose();
        }
    }
}
