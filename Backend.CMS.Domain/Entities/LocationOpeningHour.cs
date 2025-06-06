using Backend.CMS.Domain.Common;
using System;

namespace Backend.CMS.Domain.Entities
{
    public class LocationOpeningHour : BaseEntity
    {
        public Guid LocationId { get; set; }
        public Location Location { get; set; } = null!;
        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly OpenTime { get; set; }
        public TimeOnly CloseTime { get; set; }
        public bool IsClosed { get; set; }
        public bool IsOpen24Hours { get; set; }
        public string? Notes { get; set; }
    }
}