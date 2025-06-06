﻿// File: Backend.CMS.Application/DTOs/Components/ComponentDto.cs
using Backend.CMS.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Backend.CMS.Application.DTOs.Components
{
    public class ComponentLibraryDto
    {
        public List<ComponentCategoryDto> Categories { get; set; } = new();
    }

    public class ComponentCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public List<ComponentTemplateDto> Templates { get; set; } = new();
    }

    public class ComponentTemplateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ComponentType Type { get; set; }
        public string? Icon { get; set; }
        public string? Category { get; set; }
        public Dictionary<string, object> DefaultProperties { get; set; } = new();
        public Dictionary<string, object> DefaultStyles { get; set; } = new();
        public Dictionary<string, object> Schema { get; set; } = new();
        public string? PreviewHtml { get; set; }
        public string? PreviewImage { get; set; }
        public bool IsSystemTemplate { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public string? Tags { get; set; }
        public Dictionary<string, object> ConfigSchema { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateComponentTemplateDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ComponentType Type { get; set; }
        public string? Icon { get; set; }
        public string? Category { get; set; }
        public Dictionary<string, object> DefaultProperties { get; set; } = new();
        public Dictionary<string, object> DefaultStyles { get; set; } = new();
        public Dictionary<string, object> Schema { get; set; } = new();
        public string? PreviewHtml { get; set; }
        public string? PreviewImage { get; set; }
        public bool IsSystemTemplate { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public string? Tags { get; set; }
        public Dictionary<string, object> ConfigSchema { get; set; } = new();
    }

    public class UpdateComponentTemplateDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ComponentType Type { get; set; }
        public string? Icon { get; set; }
        public string? Category { get; set; }
        public Dictionary<string, object> DefaultProperties { get; set; } = new();
        public Dictionary<string, object> DefaultStyles { get; set; } = new();
        public Dictionary<string, object> Schema { get; set; } = new();
        public string? PreviewHtml { get; set; }
        public string? PreviewImage { get; set; }
        public bool IsSystemTemplate { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public string? Tags { get; set; }
        public Dictionary<string, object> ConfigSchema { get; set; } = new();
    }

    public class ComponentTemplateListDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public ComponentType Type { get; set; }
        public string? Icon { get; set; }
        public string? Category { get; set; }
        public bool IsSystemTemplate { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}