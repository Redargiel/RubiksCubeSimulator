using DataAccesLayer.Entities;

namespace DataAccesLayer.Mappers
{
    public class GameEntityMapper : IEntityMapper<GameEntity>
    {
        public void MapToExistingEntity(GameEntity existingEntity, GameEntity newEntity)
        {
            existingEntity.GameId = newEntity.GameId;
        }

    }
}
