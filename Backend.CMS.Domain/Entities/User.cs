using Backend.CMS.Domain.Common;
using Backend.CMS.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Backend.CMS.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Customer;
        public bool IsActive { get; set; } = true;
        public bool IsLocked { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public string? TwoFactorSecret { get; set; }
        public List<string> RecoveryCodes { get; set; } = [];

        // Profile enhancements
        public string? Avatar { get; set; }
        public string? Timezone { get; set; }
        public string? Language { get; set; }
        public DateTime? EmailVerifiedAt { get; set; }
        public string? EmailVerificationToken { get; set; }
        public DateTime? PasswordChangedAt { get; set; }

        // Address information (for future e-commerce)
        public string? BillingAddress { get; set; }
        public string? BillingCity { get; set; }
        public string? BillingState { get; set; }
        public string? BillingCountry { get; set; }
        public string? BillingPostalCode { get; set; }
        public string? ShippingAddress { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingState { get; set; }
        public string? ShippingCountry { get; set; }
        public string? ShippingPostalCode { get; set; }
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }

        // Preferences stored as JSON
        public Dictionary<string, object> Preferences { get; set; } = [];

        // Navigation properties
        public ICollection<UserSession> Sessions { get; set; } = [];
        public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = [];

        // Helper properties
        public bool IsAdmin => Role == UserRole.Admin;
        public bool IsCustomer => Role == UserRole.Customer;
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string RoleDisplayName => Role.ToString();
    }
}