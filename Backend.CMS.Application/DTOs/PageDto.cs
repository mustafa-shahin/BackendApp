using Backend.CMS.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Backend.CMS.Application.DTOs.Pages
{
    public class PageDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }
        public PageStatus Status { get; set; }
        public string? Template { get; set; }
        public int? Priority { get; set; }
        public Guid? ParentPageId { get; set; }
        public DateTime? PublishedOn { get; set; }
        public string? PublishedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<PageComponentDto> Components { get; set; } = new();
        public List<PageDto> ChildPages { get; set; } = new();
    }

    public class CreatePageDto
    {
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }
        public PageStatus Status { get; set; } = PageStatus.Draft;
        public string? Template { get; set; }
        public int? Priority { get; set; }
        public Guid? ParentPageId { get; set; }
    }

    public class UpdatePageDto
    {
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }
        public PageStatus Status { get; set; }
        public string? Template { get; set; }
        public int? Priority { get; set; }
        public Guid? ParentPageId { get; set; }
    }

    public class PageComponentDto
    {
        public Guid Id { get; set; }
        public ComponentType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new();
        public Dictionary<string, object> Styles { get; set; } = new();
        public Dictionary<string, object> Content { get; set; } = new();
        public int Order { get; set; }
        public Guid? ParentComponentId { get; set; }
        public List<PageComponentDto> ChildComponents { get; set; } = new();
        public bool IsVisible { get; set; } = true;
        public string? CssClasses { get; set; }
        public string? CustomCss { get; set; }
        public Dictionary<string, object> ResponsiveSettings { get; set; } = new();
        public Dictionary<string, object> AnimationSettings { get; set; } = new();
        public Dictionary<string, object> InteractionSettings { get; set; } = new();
    }

    public class SavePageStructureDto
    {
        public Guid PageId { get; set; }
        public List<PageComponentDto> Components { get; set; } = new();
    }

    public class PageListDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public PageStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? PublishedOn { get; set; }
        public bool HasChildren { get; set; }
    }
}