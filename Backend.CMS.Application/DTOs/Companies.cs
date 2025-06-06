using System;
using System.Collections.Generic;

namespace Backend.CMS.Application.DTOs.Companies
{
    public class CompanyDto
    {
        public Guid Id { get; set; }
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
        public bool IsActive { get; set; }
        public string? Timezone { get; set; }
        public string? Currency { get; set; }
        public string? Language { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<LocationDto> Locations { get; set; } = new();
    }

    public class UpdateCompanyDto
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
        public string? Timezone { get; set; }
        public string? Currency { get; set; }
        public string? Language { get; set; }
    }

    public class LocationDto
    {
        public Guid Id { get; set; }
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
        public bool IsActive { get; set; }
        public List<LocationOpeningHourDto> OpeningHours { get; set; } = new();
        public Dictionary<string, object> AdditionalInfo { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateLocationDto
    {
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
        public List<CreateLocationOpeningHourDto> OpeningHours { get; set; } = new();
        public Dictionary<string, object> AdditionalInfo { get; set; } = new();
    }

    public class UpdateLocationDto
    {
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
        public bool IsActive { get; set; }
        public List<UpdateLocationOpeningHourDto> OpeningHours { get; set; } = new();
        public Dictionary<string, object> AdditionalInfo { get; set; } = new();
    }

    public class LocationOpeningHourDto
    {
        public Guid Id { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly OpenTime { get; set; }
        public TimeOnly CloseTime { get; set; }
        public bool IsClosed { get; set; }
        public bool IsOpen24Hours { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateLocationOpeningHourDto
    {
        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly OpenTime { get; set; }
        public TimeOnly CloseTime { get; set; }
        public bool IsClosed { get; set; }
        public bool IsOpen24Hours { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateLocationOpeningHourDto
    {
        public Guid? Id { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly OpenTime { get; set; }
        public TimeOnly CloseTime { get; set; }
        public bool IsClosed { get; set; }
        public bool IsOpen24Hours { get; set; }
        public string? Notes { get; set; }
    }
}