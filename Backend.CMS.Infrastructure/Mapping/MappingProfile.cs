using AutoMapper;
using Backend.CMS.Application.DTOs.Companies;
using Backend.CMS.Application.DTOs.Components;
using Backend.CMS.Application.DTOs.Pages;
using Backend.CMS.Application.DTOs.Users;
using Backend.CMS.Application.Interfaces.Services;
using Backend.CMS.Domain.Entities;

namespace Backend.CMS.Infrastructure.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User mappings
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.RoleDisplayName, opt => opt.MapFrom(src => src.RoleDisplayName))
                .ForMember(dest => dest.IsAdmin, opt => opt.MapFrom(src => src.IsAdmin))
                .ForMember(dest => dest.IsCustomer, opt => opt.MapFrom(src => src.IsCustomer));

            CreateMap<User, UserListDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.RoleDisplayName, opt => opt.MapFrom(src => src.RoleDisplayName))
                .ForMember(dest => dest.IsAdmin, opt => opt.MapFrom(src => src.IsAdmin));

            CreateMap<CreateUserDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => BCrypt.Net.BCrypt.HashPassword(src.Password)))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            CreateMap<UpdateUserDto, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());

            // Page mappings
            CreateMap<Page, PageDto>()
                .ForMember(dest => dest.Components, opt => opt.MapFrom(src => src.Components.Where(c => !c.IsDeleted && c.ParentComponentId == null).OrderBy(c => c.Order)))
                .ForMember(dest => dest.ChildPages, opt => opt.MapFrom(src => src.ChildPages.Where(cp => !cp.IsDeleted).OrderBy(cp => cp.Priority).ThenBy(cp => cp.Name)))
                .ForMember(dest => dest.IsPublic, opt => opt.MapFrom(src => src.IsPublic))
                .ForMember(dest => dest.RequiresLogin, opt => opt.MapFrom(src => src.RequiresLogin))
                .ForMember(dest => dest.RequiresAdmin, opt => opt.MapFrom(src => src.RequiresAdmin))
                .ForMember(dest => dest.IsPublished, opt => opt.MapFrom(src => src.IsPublished));

            CreateMap<Page, PageListDto>()
                .ForMember(dest => dest.HasChildren, opt => opt.MapFrom(src => src.ChildPages.Any(cp => !cp.IsDeleted)))
                .ForMember(dest => dest.IsPublic, opt => opt.MapFrom(src => src.IsPublic))
                .ForMember(dest => dest.RequiresLogin, opt => opt.MapFrom(src => src.RequiresLogin))
                .ForMember(dest => dest.RequiresAdmin, opt => opt.MapFrom(src => src.RequiresAdmin))
                .ForMember(dest => dest.IsPublished, opt => opt.MapFrom(src => src.IsPublished));

            CreateMap<CreatePageDto, Page>()
                .ForMember(dest => dest.Components, opt => opt.Ignore())
                .ForMember(dest => dest.ChildPages, opt => opt.Ignore())
                .ForMember(dest => dest.ParentPage, opt => opt.Ignore());

            CreateMap<UpdatePageDto, Page>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Components, opt => opt.Ignore())
                .ForMember(dest => dest.ChildPages, opt => opt.Ignore())
                .ForMember(dest => dest.ParentPage, opt => opt.Ignore());

            // PageComponent mappings
            CreateMap<PageComponent, PageComponentDto>()
                .ForMember(dest => dest.ChildComponents, opt => opt.MapFrom(src => src.ChildComponents.Where(cc => !cc.IsDeleted).OrderBy(cc => cc.Order)));

            CreateMap<PageComponentDto, PageComponent>()
                .ForMember(dest => dest.Page, opt => opt.Ignore())
                .ForMember(dest => dest.ParentComponent, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedBy, opt => opt.Ignore());

            // PageVersion mappings
            CreateMap<PageVersion, PageVersionDto>()
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedByUser.FirstName + " " + src.CreatedByUser.LastName));

            // Company mappings
            CreateMap<Company, CompanyDto>()
                .ForMember(dest => dest.Locations, opt => opt.MapFrom(src => src.Locations.Where(l => !l.IsDeleted)));

            CreateMap<UpdateCompanyDto, Company>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Locations, opt => opt.Ignore());

            // Location mappings
            CreateMap<Location, LocationDto>()
                .ForMember(dest => dest.OpeningHours, opt => opt.MapFrom(src => src.OpeningHours.Where(oh => !oh.IsDeleted)));

            CreateMap<CreateLocationDto, Location>()
                .ForMember(dest => dest.Company, opt => opt.Ignore())
                .ForMember(dest => dest.OpeningHours, opt => opt.Ignore());

            CreateMap<UpdateLocationDto, Location>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Company, opt => opt.Ignore())
                .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
                .ForMember(dest => dest.OpeningHours, opt => opt.Ignore());

            // LocationOpeningHour mappings
            CreateMap<LocationOpeningHour, LocationOpeningHourDto>();
            CreateMap<CreateLocationOpeningHourDto, LocationOpeningHour>()
                .ForMember(dest => dest.Location, opt => opt.Ignore())
                .ForMember(dest => dest.LocationId, opt => opt.Ignore());

            // ComponentTemplate mappings
            CreateMap<ComponentTemplate, ComponentTemplateDto>();

            CreateMap<CreateComponentTemplateDto, ComponentTemplate>()
                .ForMember(dest => dest.IsSystemTemplate, opt => opt.MapFrom(src => false));

            CreateMap<UpdateComponentTemplateDto, ComponentTemplate>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.IsSystemTemplate, opt => opt.Ignore());
        }
    }
}