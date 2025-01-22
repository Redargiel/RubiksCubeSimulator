using DataAccesLayer.Common.Enums;

namespace DataAccesLayer.Entities
{
    public record SidePointEntity : IEntity
    {
        public required int PointId { get; set; }
        public required int SideId { get; set; }
        public required ColorEnum Color { get; set; }
        public required Guid ID { get; set; }
    }
}
