using DataAccesLayer.Entities;

namespace DataAccesLayer.Mappers
{
    public class CubeStateEntityMapper : IEntityMapper<CubeStateEntity>
    {
        public void MapToExistingEntity(CubeStateEntity existingEntity, CubeStateEntity newEntity)
        {
            existingEntity.StateId = newEntity.StateId;
            existingEntity.GameId = newEntity.GameId;
        }

    }
}
