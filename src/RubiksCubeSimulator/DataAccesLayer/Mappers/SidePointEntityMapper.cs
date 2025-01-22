using DataAccesLayer.Entities;

namespace DataAccesLayer.Mappers
{
    public class SidePointEntityMapper : IEntityMapper<SidePointEntity>
    {
        public void MapToExistingEntity(SidePointEntity existingEntity, SidePointEntity newEntity)
        {
            existingEntity.PointId = newEntity.PointId;
            existingEntity.SideId = newEntity.SideId;
            existingEntity.Color = newEntity.Color;
        }

    }
}

