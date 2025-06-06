using AutoMapper;
using Backend.CMS.Application.DTOs.Companies;
using Backend.CMS.Application.Interfaces.Services;
using Backend.CMS.Domain.Entities;
using Backend.CMS.Infrastructure.Repositories;

namespace Backend.CMS.Infrastructure.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly IRepository<Company> _companyRepository;
        private readonly IRepository<Location> _locationRepository;
        private readonly IMapper _mapper;

        public CompanyService(
            IRepository<Company> companyRepository,
            IRepository<Location> locationRepository,
            IMapper mapper)
        {
            _companyRepository = companyRepository;
            _locationRepository = locationRepository;
            _mapper = mapper;
        }

        public async Task<CompanyDto> GetCompanyAsync()
        {
            var companies = await _companyRepository.GetAllAsync();
            var company = companies.FirstOrDefault();

            if (company == null)
                throw new ArgumentException("Company not found");

            return _mapper.Map<CompanyDto>(company);
        }

        public async Task<CompanyDto> UpdateCompanyAsync(UpdateCompanyDto updateCompanyDto)
        {
            var companies = await _companyRepository.GetAllAsync();
            var company = companies.FirstOrDefault();

            if (company == null)
                throw new ArgumentException("Company not found");

            _mapper.Map(updateCompanyDto, company);
            _companyRepository.Update(company);
            await _companyRepository.SaveChangesAsync();

            return _mapper.Map<CompanyDto>(company);
        }

        public async Task<LocationDto> GetLocationByIdAsync(Guid locationId)
        {
            var location = await _locationRepository.GetByIdAsync(locationId);
            if (location == null)
                throw new ArgumentException("Location not found");

            return _mapper.Map<LocationDto>(location);
        }

        public async Task<List<LocationDto>> GetLocationsAsync()
        {
            var locations = await _locationRepository.GetAllAsync();
            return _mapper.Map<List<LocationDto>>(locations);
        }

        public async Task<LocationDto> CreateLocationAsync(CreateLocationDto createLocationDto)
        {
            var location = _mapper.Map<Location>(createLocationDto);
            await _locationRepository.AddAsync(location);
            await _locationRepository.SaveChangesAsync();

            return _mapper.Map<LocationDto>(location);
        }

        public async Task<LocationDto> UpdateLocationAsync(Guid locationId, UpdateLocationDto updateLocationDto)
        {
            var location = await _locationRepository.GetByIdAsync(locationId);
            if (location == null)
                throw new ArgumentException("Location not found");

            _mapper.Map(updateLocationDto, location);
            _locationRepository.Update(location);
            await _locationRepository.SaveChangesAsync();

            return _mapper.Map<LocationDto>(location);
        }

        public async Task<bool> DeleteLocationAsync(Guid locationId)
        {
            var location = await _locationRepository.GetByIdAsync(locationId);
            if (location == null)
                return false;

            _locationRepository.Remove(location);
            await _locationRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetMainLocationAsync(Guid locationId)
        {
            var location = await _locationRepository.GetByIdAsync(locationId);
            if (location == null)
                return false;

            // Unset all other main locations
            var allLocations = await _locationRepository.GetAllAsync();
            foreach (var loc in allLocations)
            {
                loc.IsMainLocation = false;
                _locationRepository.Update(loc);
            }

            // Set this location as main
            location.IsMainLocation = true;
            _locationRepository.Update(location);
            await _locationRepository.SaveChangesAsync();
            return true;
        }

        public async Task<LocationDto> GetMainLocationAsync()
        {
            var locations = await _locationRepository.FindAsync(l => l.IsMainLocation);
            var mainLocation = locations.FirstOrDefault();

            if (mainLocation == null)
                throw new ArgumentException("Main location not found");

            return _mapper.Map<LocationDto>(mainLocation);
        }
    }
}