using Backend.CMS.Domain.Common;
using Backend.CMS.Domain.Common.Interfaces;
using Backend.CMS.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Backend.CMS.Domain.Entities
{
    public class ComponentTemplate : BaseEntity, ITenantEntity
    {
        public string TenantId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ComponentType Type { get; set; }
        public string? Icon { get; set; }
        public string? Category { get; set; }
        public Dictionary<string, object> DefaultProperties { get; set; } = new();
        public Dictionary<string, object> DefaultStyles { get; set; } = new();
        public Dictionary<string, object> Schema { get; set; } = new(); // JSON Schema for validation
        public string? PreviewHtml { get; set; }
        public string? PreviewImage { get; set; }
        public bool IsSystemTemplate { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public string? Tags { get; set; }
        public Dictionary<string, object> ConfigSchema { get; set; } = new();
    }
}