using Backend.CMS.Domain.Common;
using System;
using System.Collections.Generic;

namespace Backend.CMS.Domain.Entities
{
    public class Company : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Website { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
        public string? Logo { get; set; }
        public string? Favicon { get; set; }
        public Dictionary<string, object> BrandingSettings { get; set; } = new();
        public Dictionary<string, object> SocialMediaLinks { get; set; } = new();
        public Dictionary<string, object> ContactInfo { get; set; } = new();
        public Dictionary<string, object> BusinessSettings { get; set; } = new();
        public ICollection<Location> Locations { get; set; } = new List<Location>();
        public bool IsActive { get; set; } = true;
        public string? Timezone { get; set; }
        public string? Currency { get; set; }
        public string? Language { get; set; }
    }
}