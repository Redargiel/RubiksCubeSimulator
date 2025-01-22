using DataAccesLayer.Entities;

namespace DataAccesLayer.Mappers
{
    public interface IEntityMapper<in TEntity>
    where TEntity : IEntity
    {
        void MapToExistingEntity(TEntity existingEntity, TEntity newEntity);
    }
}
