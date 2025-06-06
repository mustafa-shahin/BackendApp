using Backend.CMS.Application.DTOs.Companies;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.CMS.Application.Interfaces.Services
{
    public interface ICompanyService
    {
        Task<CompanyDto> GetCompanyAsync();
        Task<CompanyDto> UpdateCompanyAsync(UpdateCompanyDto updateCompanyDto);
        Task<LocationDto> GetLocationByIdAsync(Guid locationId);
        Task<List<LocationDto>> GetLocationsAsync();
        Task<LocationDto> CreateLocationAsync(CreateLocationDto createLocationDto);
        Task<LocationDto> UpdateLocationAsync(Guid locationId, UpdateLocationDto updateLocationDto);
        Task<bool> DeleteLocationAsync(Guid locationId);
        Task<bool> SetMainLocationAsync(Guid locationId);
        Task<LocationDto> GetMainLocationAsync();
    }
}