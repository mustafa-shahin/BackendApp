using Backend.CMS.Domain.Common;
using Backend.CMS.Domain.Common.Interfaces;
using System;
using System.Collections.Generic;

namespace Backend.CMS.Domain.Entities
{
    public class Location : BaseEntity, ITenantEntity
    {
        public string TenantId { get; set; } = string.Empty;
        public Guid CompanyId { get; set; }
        public Company Company { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public bool IsMainLocation { get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<LocationOpeningHour> OpeningHours { get; set; } = new List<LocationOpeningHour>();
        public Dictionary<string, object> AdditionalInfo { get; set; } = new();
    }
}