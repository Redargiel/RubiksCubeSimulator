using DataAccesLayer.Common.Enums;
namespace DataAccesLayer.Entities
{
    public record GameEntity : IEntity
    {
        public required int GameId { get; set; }
        public required Guid ID { get; set; }
    }
}
