using DataAccesLayer.Common.Enums;

namespace DataAccesLayer.Entities
{
    public record CubeStateEntity : IEntity
    {
        public required int StateId { get; set; }
        public required int GameId { get; set; }
        public required Guid ID { get; set; }
}
}
