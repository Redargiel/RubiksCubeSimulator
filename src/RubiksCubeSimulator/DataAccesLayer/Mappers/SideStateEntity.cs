using DataAccesLayer.Entities;

namespace DataAccesLayer.Mappers
{
    public class SideStateMapper : IEntityMapper<SideStateEntity>
    {
        public void MapToExistingEntity(SideStateEntity existingEntity, SideStateEntity newEntity)
        {
            existingEntity.StateId = newEntity.StateId;
            existingEntity.SideId = newEntity.SideId;
            existingEntity.Colors = newEntity.Colors;
        }

    }
}
