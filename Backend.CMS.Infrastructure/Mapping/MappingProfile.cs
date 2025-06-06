// File: Backend.CMS.Infrastructure/Mapping/MappingProfile.cs
using AutoMapper;
using Backend.CMS.Application.DTOs.Companies;
using Backend.CMS.Application.DTOs.Components;
using Backend.CMS.Application.DTOs.Pages;
using Backend.CMS.Application.DTOs.Users;
using Backend.CMS.Application.Interfaces.Services;
using Backend.CMS.Domain.Entities;
using System.Data;
using System.Security;

namespace Backend.CMS.Infrastructure.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User mappings
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role)));

            CreateMap<User, UserListDto>()
                .ForMember(dest => dest.RoleNames, opt => opt.MapFrom(src => src.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.Name).ToList()));

            CreateMap<CreateUserDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => BCrypt.Net.BCrypt.HashPassword(src.Password)))
                .ForMember(dest => dest.UserRoles, opt => opt.Ignore());

            CreateMap<UpdateUserDto, User>()
                .ForMember(dest => dest.UserRoles, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore());

            // Role mappings
            CreateMap<Role, RoleDto>()
                .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.RolePermissions.Select(rp => rp.Permission)));

            // Permission mappings
            CreateMap<Permission, PermissionDto>();

            // Page mappings
            CreateMap<Page, PageDto>()
                .ForMember(dest => dest.Components, opt => opt.MapFrom(src => src.Components.Where(c => !c.IsDeleted && c.ParentComponentId == null).OrderBy(c => c.Order)))
                .ForMember(dest => dest.ChildPages, opt => opt.MapFrom(src => src.ChildPages.Where(cp => !cp.IsDeleted).OrderBy(cp => cp.Priority).ThenBy(cp => cp.Name)));

            CreateMap<Page, PageListDto>()
                .ForMember(dest => dest.HasChildren, opt => opt.MapFrom(src => src.ChildPages.Any(cp => !cp.IsDeleted)));

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
                .ForMember(dest => dest.Locations, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore());

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
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.OpeningHours, opt => opt.Ignore());

            // LocationOpeningHour mappings
            CreateMap<LocationOpeningHour, LocationOpeningHourDto>();
            CreateMap<CreateLocationOpeningHourDto, LocationOpeningHour>()
                .ForMember(dest => dest.Location, opt => opt.Ignore())
                .ForMember(dest => dest.LocationId, opt => opt.Ignore());

            // ComponentTemplate mappings (Using Components namespace to avoid ambiguity)
            CreateMap<ComponentTemplate, ComponentTemplateDto>();

            CreateMap<CreateComponentTemplateDto, ComponentTemplate>()
                .ForMember(dest => dest.IsSystemTemplate, opt => opt.MapFrom(src => false));

            CreateMap<UpdateComponentTemplateDto, ComponentTemplate>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.IsSystemTemplate, opt => opt.Ignore());
        }
    }
}