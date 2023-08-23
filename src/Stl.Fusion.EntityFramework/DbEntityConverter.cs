using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.EntityFramework;

public interface IDbEntityConverter<TDbEntity, TModel>
    where TDbEntity : class
    where TModel : notnull
{
    TDbEntity NewEntity();
    TModel NewModel();

    void UpdateEntity(TModel source, TDbEntity target);
    TModel UpdateModel(TDbEntity source, TModel target);

#if !NETSTANDARD2_0
    [return: NotNullIfNotNull("source")]
#endif
    TDbEntity? ToEntity(TModel? source);

#if !NETSTANDARD2_0
    [return: NotNullIfNotNull("source")]
#endif
    TModel? ToModel(TDbEntity? source);
}

public abstract class DbEntityConverter<TDbContext, TDbEntity, TModel>(IServiceProvider services)
    : DbServiceBase<TDbContext>(services), IDbEntityConverter<TDbEntity, TModel>
    where TDbContext : DbContext
    where TDbEntity : class
    where TModel : notnull
{
    public abstract TDbEntity NewEntity();
    public abstract TModel NewModel();

    public abstract void UpdateEntity(TModel source, TDbEntity target);
    public abstract TModel UpdateModel(TDbEntity source, TModel target);

#if !NETSTANDARD2_0
    [return: NotNullIfNotNull("source")]
#endif
    public virtual TDbEntity? ToEntity(TModel? source)
    {
        if (source == null)
            return null;
        var dbEntity = NewEntity();
        UpdateEntity(source, dbEntity);
        return dbEntity;
    }

#if !NETSTANDARD2_0
    [return: NotNullIfNotNull("source")]
#endif
    public virtual TModel? ToModel(TDbEntity? source)
    {
        if (source == null)
            return default;
        var model = NewModel();
        model = UpdateModel(source, model);
        return model;
    }
}
