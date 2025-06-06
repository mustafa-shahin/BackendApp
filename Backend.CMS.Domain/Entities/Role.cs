﻿using Backend.CMS.Domain.Common;
using Backend.CMS.Domain.Common.Interfaces;
using System;
using System.Collections.Generic;

namespace Backend.CMS.Domain.Entities
{
    public class Role : BaseEntity, ITenantEntity
    {
        public string TenantId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string NormalizedName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}