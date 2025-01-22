using DataAccesLayer.Common.Enums;

namespace DataAccesLayer.Entities
{
    public record SideStateEntity : IEntity
    {
        public required int StateId { get; set; }
        public required int SideId { get; set; }
        public required string Colors { get; set; }
        public required Guid ID { get; set; }
    }
}
