using Backend.CMS.Domain.Common;
using Backend.CMS.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Backend.CMS.Domain.Entities
{
    public class PageComponent : BaseEntity
    {
        public Guid PageId { get; set; }
        public Page Page { get; set; } = null!;
        public ComponentType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new();
        public Dictionary<string, object> Styles { get; set; } = new();
        public Dictionary<string, object> Content { get; set; } = new();
        public int Order { get; set; }
        public Guid? ParentComponentId { get; set; }
        public PageComponent? ParentComponent { get; set; }
        public ICollection<PageComponent> ChildComponents { get; set; } = new List<PageComponent>();
        public bool IsVisible { get; set; } = true;
        public string? CssClasses { get; set; }
        public string? CustomCss { get; set; }
        public Dictionary<string, object> ResponsiveSettings { get; set; } = new();
        public Dictionary<string, object> AnimationSettings { get; set; } = new();
        public Dictionary<string, object> InteractionSettings { get; set; } = new();
    }
}