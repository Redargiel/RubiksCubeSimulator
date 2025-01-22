using DataAccesLayer.Entities;
using Microsoft.EntityFrameworkCore;
using DataAccesLayer.Mappers;
using System.Linq.Expressions;
using DataAccesLayer.Entities;
using DataAccesLayer.Mappers;
using System.Collections.Generic;

namespace DataAccessLayer.Repositories;

public class Repository<TEntity>(
    DbContext dbContext,
    IEntityMapper<TEntity> entityMapper)
    : IRepository<TEntity>
    where TEntity : class, IEntity
{
    private readonly DbSet<TEntity> dbSet = dbContext.Set<TEntity>();

    public IQueryable<TEntity> Get() => dbSet;

    public async Task<bool> ExistsAsync(TEntity entity)
        => entity.ID != Guid.Empty
           && await dbSet.AnyAsync(e => e.ID == entity.ID).ConfigureAwait(false);

    public TEntity Insert(TEntity entity)
        => dbSet.Add(entity).Entity;
    public async Task<List<TEntity>> GetAllWhere(Func<TEntity, bool> predicate)
    {
        return await Task.FromResult(dbSet.Where(predicate).ToList()) ?? throw new Exception("No entites found");
    }
    public async Task<TEntity> GetByIdAsync(Guid id)
    {
        TEntity? entity = await dbSet.FirstOrDefaultAsync(x => x.ID == id);
        if (entity == null)
            throw new NullReferenceException(nameof(entity));
        return entity;
    }
    public async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await dbSet.ToListAsync() ?? throw new Exception("No entites found");
    }
    public async Task<TEntity> InsertAsync(TEntity entity)
    {
        var addedEntity = await dbSet.AddAsync(entity);
        await dbContext.SaveChangesAsync();
        return addedEntity.Entity;
    }
    public IList<TEntity> GetAllIncluding(params Expression<Func<TEntity, object>>[] includeProperties)
    {
        IQueryable<TEntity> query = dbSet;
        foreach (var includeProperty in includeProperties)
        {
            query = query.Include(includeProperty);
        }
        return query.ToList();
    }
    public async Task<IList<TEntity>> GetAllWhereIncluding(
        Expression<Func<TEntity, bool>> predicate,
        params Expression<Func<TEntity, object>>[] includeProperties)
    {
        IQueryable<TEntity> query = dbSet;

        foreach (var includeProperty in includeProperties)
        {
            query = query.Include(includeProperty);
        }

        query = query.Where(predicate);

        return await query.ToListAsync();
    }
    public async Task<TEntity> UpdateAsync(TEntity entity)
    {
        TEntity existingEntity = await dbSet.SingleAsync(e => e.ID == entity.ID).ConfigureAwait(false);
        entityMapper.MapToExistingEntity(existingEntity, entity);
        return existingEntity;
    }

    public async Task DeleteAsync(Guid entityId)
        => dbSet.Remove(await dbSet.SingleAsync(i => i.ID == entityId).ConfigureAwait(false));
    public void Dispose()
    {
        dbContext?.Dispose();
    }

}
