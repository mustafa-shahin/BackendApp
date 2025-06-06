// File: Backend.CMS.Infrastructure/Validation/DtoValidators.cs
using Backend.CMS.Application.DTOs.Companies;
using Backend.CMS.Application.DTOs.Components;
using Backend.CMS.Application.DTOs.Pages;
using Backend.CMS.Application.DTOs.Users;
using FluentValidation;

namespace Backend.CMS.Infrastructure.Validation
{
    public class CreatePageDtoValidator : AbstractValidator<CreatePageDto>
    {
        public CreatePageDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Page name is required")
                .MaximumLength(200).WithMessage("Page name cannot exceed 200 characters");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Page title is required")
                .MaximumLength(200).WithMessage("Page title cannot exceed 200 characters");

            RuleFor(x => x.Slug)
                .NotEmpty().WithMessage("Page slug is required")
                .MaximumLength(200).WithMessage("Page slug cannot exceed 200 characters")
                .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be lowercase letters, numbers, and hyphens only");

            RuleFor(x => x.MetaTitle)
                .MaximumLength(200).WithMessage("Meta title cannot exceed 200 characters")
                .When(x => !string.IsNullOrEmpty(x.MetaTitle));

            RuleFor(x => x.MetaDescription)
                .MaximumLength(500).WithMessage("Meta description cannot exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.MetaDescription));
        }
    }

    public class UpdatePageDtoValidator : AbstractValidator<UpdatePageDto>
    {
        public UpdatePageDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Page name is required")
                .MaximumLength(200).WithMessage("Page name cannot exceed 200 characters");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Page title is required")
                .MaximumLength(200).WithMessage("Page title cannot exceed 200 characters");

            RuleFor(x => x.Slug)
                .NotEmpty().WithMessage("Page slug is required")
                .MaximumLength(200).WithMessage("Page slug cannot exceed 200 characters")
                .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be lowercase letters, numbers, and hyphens only");

            RuleFor(x => x.MetaTitle)
                .MaximumLength(200).WithMessage("Meta title cannot exceed 200 characters")
                .When(x => !string.IsNullOrEmpty(x.MetaTitle));

            RuleFor(x => x.MetaDescription)
                .MaximumLength(500).WithMessage("Meta description cannot exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.MetaDescription));
        }
    }

    public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
    {
        public CreateUserDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(256).WithMessage("Email cannot exceed 256 characters");

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters")
                .MaximumLength(256).WithMessage("Username cannot exceed 256 characters")
                .Matches(@"^[a-zA-Z0-9_.-]+$").WithMessage("Username can only contain letters, numbers, underscores, hyphens, and periods");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]").WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");
        }
    }

    public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
    {
        public UpdateUserDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(256).WithMessage("Email cannot exceed 256 characters");

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters")
                .MaximumLength(256).WithMessage("Username cannot exceed 256 characters")
                .Matches(@"^[a-zA-Z0-9_.-]+$").WithMessage("Username can only contain letters, numbers, underscores, hyphens, and periods");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");
        }
    }

    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");
        }
    }

    public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
    {
        public ChangePasswordDtoValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Current password is required");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]").WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Confirm password is required")
                .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
        }
    }

    public class UpdateCompanyDtoValidator : AbstractValidator<UpdateCompanyDto>
    {
        public UpdateCompanyDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Company name is required")
                .MaximumLength(200).WithMessage("Company name cannot exceed 200 characters");

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(256).WithMessage("Email cannot exceed 256 characters")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.Phone)
                .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters")
                .When(x => !string.IsNullOrEmpty(x.Phone));

            RuleFor(x => x.Website)
                .Must(BeAValidUrl).WithMessage("Invalid website URL")
                .MaximumLength(500).WithMessage("Website URL cannot exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Website));
        }

        private bool BeAValidUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return true;
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }
    }

    public class CreateLocationDtoValidator : AbstractValidator<CreateLocationDto>
    {
        public CreateLocationDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Location name is required")
                .MaximumLength(200).WithMessage("Location name cannot exceed 200 characters");

            RuleFor(x => x.Address)
                .NotEmpty().WithMessage("Address is required")
                .MaximumLength(500).WithMessage("Address cannot exceed 500 characters");

            RuleFor(x => x.City)
                .NotEmpty().WithMessage("City is required")
                .MaximumLength(100).WithMessage("City cannot exceed 100 characters");

            RuleFor(x => x.State)
                .NotEmpty().WithMessage("State is required")
                .MaximumLength(100).WithMessage("State cannot exceed 100 characters");

            RuleFor(x => x.Country)
                .NotEmpty().WithMessage("Country is required")
                .MaximumLength(100).WithMessage("Country cannot exceed 100 characters");

            RuleFor(x => x.PostalCode)
                .NotEmpty().WithMessage("Postal code is required")
                .MaximumLength(20).WithMessage("Postal code cannot exceed 20 characters");

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(256).WithMessage("Email cannot exceed 256 characters")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.Phone)
                .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters")
                .When(x => !string.IsNullOrEmpty(x.Phone));

            RuleFor(x => x.Website)
                .Must(BeAValidUrl).WithMessage("Invalid website URL")
                .MaximumLength(500).WithMessage("Website URL cannot exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Website));

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90")
                .When(x => x.Latitude.HasValue);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180")
                .When(x => x.Longitude.HasValue);
        }

        private bool BeAValidUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return true;
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }
    }

    public class UpdateLocationDtoValidator : AbstractValidator<UpdateLocationDto>
    {
        public UpdateLocationDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Location name is required")
                .MaximumLength(200).WithMessage("Location name cannot exceed 200 characters");

            RuleFor(x => x.Address)
                .NotEmpty().WithMessage("Address is required")
                .MaximumLength(500).WithMessage("Address cannot exceed 500 characters");

            RuleFor(x => x.City)
                .NotEmpty().WithMessage("City is required")
                .MaximumLength(100).WithMessage("City cannot exceed 100 characters");

            RuleFor(x => x.State)
                .NotEmpty().WithMessage("State is required")
                .MaximumLength(100).WithMessage("State cannot exceed 100 characters");

            RuleFor(x => x.Country)
                .NotEmpty().WithMessage("Country is required")
                .MaximumLength(100).WithMessage("Country cannot exceed 100 characters");

            RuleFor(x => x.PostalCode)
                .NotEmpty().WithMessage("Postal code is required")
                .MaximumLength(20).WithMessage("Postal code cannot exceed 20 characters");

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(256).WithMessage("Email cannot exceed 256 characters")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.Phone)
                .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters")
                .When(x => !string.IsNullOrEmpty(x.Phone));

            RuleFor(x => x.Website)
                .Must(BeAValidUrl).WithMessage("Invalid website URL")
                .MaximumLength(500).WithMessage("Website URL cannot exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Website));

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90")
                .When(x => x.Latitude.HasValue);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180")
                .When(x => x.Longitude.HasValue);
        }

        private bool BeAValidUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return true;
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }
    }

    public class CreateComponentTemplateDtoValidator : AbstractValidator<CreateComponentTemplateDto>
    {
        public CreateComponentTemplateDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Component template name is required")
                .MaximumLength(200).WithMessage("Name cannot exceed 200 characters")
                .Matches(@"^[a-z0-9-]+$").WithMessage("Name must be lowercase letters, numbers, and hyphens only");

            RuleFor(x => x.DisplayName)
                .NotEmpty().WithMessage("Display name is required")
                .MaximumLength(200).WithMessage("Display name cannot exceed 200 characters");

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Invalid component type");

            RuleFor(x => x.Category)
                .MaximumLength(100).WithMessage("Category cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.Category));

            RuleFor(x => x.Icon)
                .MaximumLength(100).WithMessage("Icon cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.Icon));
        }
    }

    public class UpdateComponentTemplateDtoValidator : AbstractValidator<UpdateComponentTemplateDto>
    {
        public UpdateComponentTemplateDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Component template name is required")
                .MaximumLength(200).WithMessage("Name cannot exceed 200 characters")
                .Matches(@"^[a-z0-9-]+$").WithMessage("Name must be lowercase letters, numbers, and hyphens only");

            RuleFor(x => x.DisplayName)
                .NotEmpty().WithMessage("Display name is required")
                .MaximumLength(200).WithMessage("Display name cannot exceed 200 characters");

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Invalid component type");

            RuleFor(x => x.Category)
                .MaximumLength(100).WithMessage("Category cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.Category));

            RuleFor(x => x.Icon)
                .MaximumLength(100).WithMessage("Icon cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.Icon));
        }
    }

    public class SavePageStructureDtoValidator : AbstractValidator<SavePageStructureDto>
    {
        public SavePageStructureDtoValidator()
        {
            RuleFor(x => x.PageId)
                .NotEmpty().WithMessage("Page ID is required");

            RuleFor(x => x.Components)
                .NotNull().WithMessage("Components list cannot be null");

            RuleForEach(x => x.Components)
                .SetValidator(new PageComponentDtoValidator());
        }
    }

    public class PageComponentDtoValidator : AbstractValidator<PageComponentDto>
    {
        public PageComponentDtoValidator()
        {
            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Invalid component type");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Component name is required")
                .MaximumLength(200).WithMessage("Component name cannot exceed 200 characters");

            RuleFor(x => x.Order)
                .GreaterThanOrEqualTo(0).WithMessage("Order must be greater than or equal to 0");

            RuleFor(x => x.Properties)
                .NotNull().WithMessage("Properties cannot be null");

            RuleFor(x => x.Styles)
                .NotNull().WithMessage("Styles cannot be null");

            RuleFor(x => x.Content)
                .NotNull().WithMessage("Content cannot be null");

            RuleForEach(x => x.ChildComponents)
                .SetValidator(new PageComponentDtoValidator());
        }
    }
}