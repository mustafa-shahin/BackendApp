
using Backend.CMS.Application.DTOs.Components;
using Backend.CMS.Domain.Enums;
namespace Backend.CMS.Application.Interfaces
{
    public interface IComponentService
    {
        Task<ComponentTemplateDto> GetComponentTemplateByIdAsync(Guid templateId);
        Task<List<ComponentTemplateDto>> GetComponentTemplatesAsync();
        Task<ComponentLibraryDto> GetComponentLibraryAsync();
        Task<List<ComponentTemplateDto>> GetComponentTemplatesByTypeAsync(ComponentType type);
        Task<List<ComponentTemplateDto>> GetComponentTemplatesByCategoryAsync(string category);
        Task<ComponentTemplateDto> CreateComponentTemplateAsync(CreateComponentTemplateDto createComponentTemplateDto);
        Task<ComponentTemplateDto> UpdateComponentTemplateAsync(Guid templateId, UpdateComponentTemplateDto updateComponentTemplateDto);
        Task<bool> DeleteComponentTemplateAsync(Guid templateId);
        Task<bool> ValidateComponentDataAsync(ComponentType type, Dictionary<string, object> data);
        Task<Dictionary<string, object>> GetDefaultPropertiesAsync(ComponentType type);
        Task<Dictionary<string, object>> GetDefaultStylesAsync(ComponentType type);
        Task<List<ComponentTemplateDto>> GetSystemTemplatesAsync();
        Task<List<ComponentTemplateDto>> GetCustomTemplatesAsync();
    }
}