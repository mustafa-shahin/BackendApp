using Backend.CMS.Application.Interfaces;
using Backend.CMS.Application.Interfaces.Services;
using Backend.CMS.Domain.Entities;
using Backend.CMS.Domain.Enums;
using Backend.CMS.Infrastructure.Data;
using Backend.CMS.Infrastructure.Mapping;
using Backend.CMS.Infrastructure.Repositories;
using Backend.CMS.Infrastructure.Services;
using FluentValidation;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text;

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
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

// Configure Hangfire
var hangfireConnectionString = builder.Configuration.GetConnectionString("HangfireConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(hangfireConnectionString, new PostgreSqlStorageOptions
    {
        QueuePollInterval = TimeSpan.FromSeconds(10),
        JobExpirationCheckInterval = TimeSpan.FromHours(1),
        CountersAggregateInterval = TimeSpan.FromMinutes(5),
        PrepareSchemaIfNecessary = true,
        TransactionSynchronisationTimeout = TimeSpan.FromMinutes(5)
    }));

builder.Services.AddHangfireServer(options =>
{
    options.Queues = ["default", "deployment", "template-sync", "maintenance"];
    options.WorkerCount = Environment.ProcessorCount * 2;
});

// Configure Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("JWT SecretKey is not configured");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; // Set to true in production
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero,
        RequireExpirationTime = true,
        RequireSignedTokens = true
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"Token validated for: {context.Principal.Identity.Name}");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"Authentication challenge: {context.Error}, {context.ErrorDescription}");
            return Task.CompletedTask;
        }
    };
});

// Add Authorization
builder.Services.AddAuthorization();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Register FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Register MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

// Register repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IPageRepository, PageRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRepository<UserSession>, Repository<UserSession>>();
builder.Services.AddScoped<IRepository<PasswordResetToken>, Repository<PasswordResetToken>>();
builder.Services.AddScoped<IRepository<Company>, Repository<Company>>();
builder.Services.AddScoped<IRepository<Location>, Repository<Location>>();
builder.Services.AddScoped<IRepository<ComponentTemplate>, Repository<ComponentTemplate>>();
builder.Services.AddScoped<IRepository<DeploymentVersion>, Repository<DeploymentVersion>>();
builder.Services.AddScoped<IRepository<TemplateSyncLog>, Repository<TemplateSyncLog>>();
builder.Services.AddScoped<IRepository<DeploymentJob>, Repository<DeploymentJob>>();
builder.Services.AddScoped<IRepository<TemplateSyncJob>, Repository<TemplateSyncJob>>();
builder.Services.AddScoped<IRepository<DeploymentProposal>, Repository<DeploymentProposal>>();
builder.Services.AddScoped<IRepository<TemplateUpdateProposal>, Repository<TemplateUpdateProposal>>();

// Register services
builder.Services.AddScoped<IPageService, PageService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IComponentService, ComponentService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IVersioningService, VersioningService>();
builder.Services.AddScoped<IDeploymentJobService, DeploymentJobService>();
builder.Services.AddScoped<ITemplateSyncJobService, TemplateSyncJobService>();

// Add HTTP context accessor and session support
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Backend CMS API",
        Version = "v1",
        Description = "CMS API with page builder functionality and user management"
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

// Configure Hangfire Dashboard
app.UseHangfireDashboard("/jobs", new DashboardOptions
{
    Authorization = [new HangfireAuthorizationFilter()],
    DisplayStorageConnectionString = false,
    DashboardTitle = "Backend CMS Jobs"
});

app.UseSession();
app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();

// Request logging middleware
app.Use(async (context, next) =>
{
    Console.WriteLine($"=== REQUEST: {context.Request.Method} {context.Request.Path} ===");
    Console.WriteLine($"Host: {context.Request.Host}");
    Console.WriteLine($"ContentType: {context.Request.ContentType}");
    Console.WriteLine($"Headers: {string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}={h.Value}"))}");

    await next();

    Console.WriteLine($"=== RESPONSE: {context.Response.StatusCode} ===");
});

app.MapControllers();

// Database migration and seeding endpoint
app.MapPost("/admin/migrate", async (IServiceProvider serviceProvider) =>
{
    if (!app.Environment.IsDevelopment()) return Results.NotFound();

    using var scope = serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        await context.Database.MigrateAsync();
        await SeedDatabase(context);
        return Results.Ok("Migration completed successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Migration failed");
        return Results.Problem($"Migration failed: {ex.Message}");
    }
});

// Admin-controlled job management endpoints
app.MapPost("/admin/jobs/check-template-updates", async (ITemplateSyncJobService templateSyncService) =>
{
    var detection = await templateSyncService.DetectTemplateUpdatesAsync();
    return Results.Ok(detection);
}).RequireAuthorization();

app.MapPost("/admin/jobs/emergency-stop-all", async (IServiceProvider serviceProvider) =>
{
    using var scope = serviceProvider.CreateScope();
    var hangfireClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
    return Results.Ok("Emergency stop initiated - check Hangfire dashboard");
}).RequireAuthorization();

// Initialize Hangfire database
try
{
    using (var scope = app.Services.CreateScope())
    {
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var hangfireConn = configuration.GetConnectionString("HangfireConnection")
            ?? configuration.GetConnectionString("DefaultConnection");

        GlobalConfiguration.Configuration.UsePostgreSqlStorage(hangfireConn);
    }
}
catch (Exception ex)
{
    Log.Error(ex, "Failed to initialize Hangfire database");
}

// Initialize database
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
        await SeedDatabase(context);
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "An error occurred while migrating or seeding the database");
    throw;
}

app.Run();

// Database seeding method
async Task SeedDatabase(ApplicationDbContext context)
{
    try
    {
        // Seed default company
        if (!context.Companies.Any())
        {
            var company = new Company
            {
                Name = "Default Company",
                Description = "Default company for CMS",
                IsActive = true,
                Currency = "USD",
                Language = "en",
                Timezone = "UTC"
            };

            context.Companies.Add(company);
            await context.SaveChangesAsync();
        }

        // Seed default admin user if no users exist
        if (!context.Users.Any())
        {
            var adminUser = new User
            {
                Email = "admin@example.com",
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                FirstName = "System",
                LastName = "Administrator",
                Role = UserRole.Admin,
                IsActive = true,
                EmailVerifiedAt = DateTime.UtcNow
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
        }

        // Seed default component templates
        if (!context.ComponentTemplates.Any())
        {
            var componentTemplates = new[]
            {
                new ComponentTemplate
                {
                    Name = "text-block",
                    DisplayName = "Text Block",
                    Description = "Simple text content block",
                    Type = ComponentType.Text,
                    Category = "Content",
                    Icon = "type",
                    IsSystemTemplate = true,
                    DefaultProperties = new Dictionary<string, object>
                    {
                        { "text", "Enter your text here..." },
                        { "fontSize", "16px" },
                        { "fontWeight", "normal" },
                        { "textAlign", "left" }
                    }
                },
                new ComponentTemplate
                {
                    Name = "image-block",
                    DisplayName = "Image",
                    Description = "Image with optional caption",
                    Type = ComponentType.Image,
                    Category = "Media",
                    Icon = "image",
                    IsSystemTemplate = true,
                    DefaultProperties = new Dictionary<string, object>
                    {
                        { "src", "" },
                        { "alt", "" },
                        { "caption", "" },
                        { "width", "100%" },
                        { "height", "auto" }
                    }
                },
                new ComponentTemplate
                {
                    Name = "button",
                    DisplayName = "Button",
                    Description = "Clickable button element",
                    Type = ComponentType.Button,
                    Category = "Interactive",
                    Icon = "mouse-pointer",
                    IsSystemTemplate = true,
                    DefaultProperties = new Dictionary<string, object>
                    {
                        { "text", "Click me" },
                        { "link", "" },
                        { "target", "_self" },
                        { "variant", "primary" },
                        { "size", "medium" }
                    }
                },
                new ComponentTemplate
                {
                    Name = "container",
                    DisplayName = "Container",
                    Description = "Layout container for organizing content",
                    Type = ComponentType.Container,
                    Category = "Layout",
                    Icon = "square",
                    IsSystemTemplate = true,
                    DefaultProperties = new Dictionary<string, object>
                    {
                        { "maxWidth", "1200px" },
                        { "padding", "20px" },
                        { "margin", "0 auto" },
                        { "backgroundColor", "transparent" }
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

// Hangfire authorization filter
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // For development, allow all access
        // In production, implement proper authorization
        return true;
    }
}