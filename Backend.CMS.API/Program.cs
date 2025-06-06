using Backend.CMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Backend.CMS.Infrastructure.Repositories;
using Backend.CMS.Application.Interfaces.Services;
using Backend.CMS.Infrastructure.Services;
using AutoMapper;
using FluentValidation.AspNetCore;
using FluentValidation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// Configure Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
        ClockSkew = TimeSpan.Zero
    };
});

// Configure Authorization
builder.Services.AddAuthorization();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" })
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Register FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Register MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

// Register repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IPageRepository, PageRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Register services
builder.Services.AddScoped<IPageService, PageService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IComponentService, ComponentService>();

// Register tenant provider
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Backend CMS API",
        Version = "v1",
        Description = "Multi-tenant CMS API with page builder functionality"
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Backend CMS API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Database migration and seeding
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        await context.Database.MigrateAsync();
        await SeedDatabase(context);
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "An error occurred while migrating or seeding the database");
        throw;
    }
}

app.Run();

// Database seeding method
async Task SeedDatabase(ApplicationDbContext context)
{
    try
    {
        // Seed default permissions
        if (!context.Permissions.Any())
        {
            var permissions = new[]
            {
                new Backend.CMS.Domain.Entities.Permission { Name = "Pages.View", Resource = "Pages", Action = "View" },
                new Backend.CMS.Domain.Entities.Permission { Name = "Pages.Create", Resource = "Pages", Action = "Create" },
                new Backend.CMS.Domain.Entities.Permission { Name = "Pages.Update", Resource = "Pages", Action = "Update" },
                new Backend.CMS.Domain.Entities.Permission { Name = "Pages.Delete", Resource = "Pages", Action = "Delete" },
                new Backend.CMS.Domain.Entities.Permission { Name = "Pages.Publish", Resource = "Pages", Action = "Publish" },
                new Backend.CMS.Domain.Entities.Permission { Name = "Users.View", Resource = "Users", Action = "View" },
                new Backend.CMS.Domain.Entities.Permission { Name = "Users.Create", Resource = "Users", Action = "Create" },
                new Backend.CMS.Domain.Entities.Permission { Name = "Users.Update", Resource = "Users", Action = "Update" },
                new Backend.CMS.Domain.Entities.Permission { Name = "Users.Delete", Resource = "Users", Action = "Delete" },
                new Backend.CMS.Domain.Entities.Permission { Name = "Company.View", Resource = "Company", Action = "View" },
                new Backend.CMS.Domain.Entities.Permission { Name = "Company.Update", Resource = "Company", Action = "Update" },
                new Backend.CMS.Domain.Entities.Permission { Name = "Components.View", Resource = "Components", Action = "View" },
                new Backend.CMS.Domain.Entities.Permission { Name = "Components.Create", Resource = "Components", Action = "Create" },
                new Backend.CMS.Domain.Entities.Permission { Name = "Components.Update", Resource = "Components", Action = "Update" },
                new Backend.CMS.Domain.Entities.Permission { Name = "Components.Delete", Resource = "Components", Action = "Delete" }
            };

            context.Permissions.AddRange(permissions);
            await context.SaveChangesAsync();
        }

        // Seed default roles
        if (!context.Roles.Any())
        {
            var adminRole = new Backend.CMS.Domain.Entities.Role
            {
                Name = "Administrator",
                NormalizedName = "ADMINISTRATOR",
                Description = "Full system access",
                TenantId = "default"
            };

            var editorRole = new Backend.CMS.Domain.Entities.Role
            {
                Name = "Editor",
                NormalizedName = "EDITOR",
                Description = "Content management access",
                TenantId = "default"
            };

            var viewerRole = new Backend.CMS.Domain.Entities.Role
            {
                Name = "Viewer",
                NormalizedName = "VIEWER",
                Description = "Read-only access",
                TenantId = "default"
            };

            context.Roles.AddRange(adminRole, editorRole, viewerRole);
            await context.SaveChangesAsync();

            // Assign all permissions to admin role
            var allPermissions = context.Permissions.ToList();
            var adminRolePermissions = allPermissions.Select(p => new Backend.CMS.Domain.Entities.RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = p.Id
            });

            context.RolePermissions.AddRange(adminRolePermissions);
            await context.SaveChangesAsync();
        }

        // Seed default component templates
        if (!context.ComponentTemplates.Any())
        {
            var componentTemplates = new[]
            {
                new Backend.CMS.Domain.Entities.ComponentTemplate
                {
                    Name = "text-block",
                    DisplayName = "Text Block",
                    Description = "Simple text content block",
                    Type = Backend.CMS.Domain.Enums.ComponentType.Text,
                    Category = "Content",
                    Icon = "type",
                    IsSystemTemplate = true,
                    TenantId = "default",
                    DefaultProperties = new Dictionary<string, object>
                    {
                        { "text", "Enter your text here..." },
                        { "fontSize", "16px" },
                        { "fontWeight", "normal" },
                        { "textAlign", "left" }
                    }
                },
                new Backend.CMS.Domain.Entities.ComponentTemplate
                {
                    Name = "image-block",
                    DisplayName = "Image",
                    Description = "Image with optional caption",
                    Type = Backend.CMS.Domain.Enums.ComponentType.Image,
                    Category = "Media",
                    Icon = "image",
                    IsSystemTemplate = true,
                    TenantId = "default",
                    DefaultProperties = new Dictionary<string, object>
                    {
                        { "src", "" },
                        { "alt", "" },
                        { "caption", "" },
                        { "width", "100%" },
                        { "height", "auto" }
                    }
                },
                new Backend.CMS.Domain.Entities.ComponentTemplate
                {
                    Name = "button",
                    DisplayName = "Button",
                    Description = "Clickable button element",
                    Type = Backend.CMS.Domain.Enums.ComponentType.Button,
                    Category = "Interactive",
                    Icon = "mouse-pointer",
                    IsSystemTemplate = true,
                    TenantId = "default",
                    DefaultProperties = new Dictionary<string, object>
                    {
                        { "text", "Click me" },
                        { "link", "" },
                        { "target", "_self" },
                        { "variant", "primary" },
                        { "size", "medium" }
                    }
                }
            };

            context.ComponentTemplates.AddRange(componentTemplates);
            await context.SaveChangesAsync();
        }

        Log.Information("Database seeded successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error occurred while seeding database");
        throw;
    }
}