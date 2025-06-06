﻿using System;
using System.Data;

namespace Backend.CMS.Domain.Entities
{
    public class UserRole
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public string AssignedBy { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}