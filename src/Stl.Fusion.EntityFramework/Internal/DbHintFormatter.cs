using System.Text;

namespace Stl.Fusion.EntityFramework.Internal;

public interface IDbHintFormatter
{
    string FormatSelectSql(string tableName, ref MemoryBuffer<DbHint> hints);
}

public abstract class DbHintFormatter : IDbHintFormatter
{
    protected Dictionary<DbHint, string> DbHintToSql { get; init; } = new();

    public abstract string FormatSelectSql(string tableName, ref MemoryBuffer<DbHint> hints);

    protected virtual string FormatHint(DbHint hint)
        => hint switch {
            DbCustomHint dbCustomHint => dbCustomHint.Value,
            _ => DbHintToSql.TryGetValue(hint, out var sql)
                ? sql
                : throw Errors.UnsupportedDbHint(hint),
        };

    protected virtual void FormatTableNameTo(StringBuilder sb, string tableName)
    {
        sb.Append('"');
        sb.Append(tableName);
        sb.Append('"');
    }
}
