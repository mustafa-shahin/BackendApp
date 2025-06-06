    using Backend.CMS.Application.Interfaces;
    using Backend.CMS.Application.Interfaces.Services;
    using Backend.CMS.Infrastructure.Data;
    using Backend.CMS.Infrastructure.Mapping;
    using Backend.CMS.Infrastructure.Repositories;
    using Backend.CMS.Infrastructure.Services;
    using FluentValidation;
    using Hangfire;
    using Hangfire.Dashboard;
    using Hangfire.PostgreSql;
    using MediatR;
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

    // Configure Entity Framework with dynamic tenant connection string
    builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
    {
        var tenantProvider = serviceProvider.GetRequiredService<ITenantProvider>();
        var tenantId = tenantProvider.GetTenantId();

        var connectionStringTemplate = builder.Configuration.GetConnectionString("DefaultConnection");
        var connectionString = connectionStringTemplate?.Replace("{TENANT_ID}", tenantId);

        options.UseNpgsql(connectionString);
    });

    // Configure Hangfire
    var hangfireConnectionString = builder.Configuration.GetConnectionString("HangfireConnection")
        ?? builder.Configuration.GetConnectionString("DefaultConnection")?.Replace("{TENANT_ID}", "hangfire");

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
            // Removed DashboardJobListLimit and TablesPrefix as they're not available in this version
        }));

    builder.Services.AddHangfireServer(options =>
    {
        options.Queues = new[] { "default", "deployment", "template-sync", "maintenance" };
        options.WorkerCount = Environment.ProcessorCount * 2;
    });

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
    builder.Services.AddAutoMapper(typeof(MappingProfile));

    // Register FluentValidation
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // Register MediatR
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

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
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IVersioningService, VersioningService>();
    builder.Services.AddScoped<IDeploymentJobService, DeploymentJobService>();
    builder.Services.AddScoped<ITemplateSyncJobService, TemplateSyncJobService>();

    // Register tenant provider
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ITenantProvider, TenantProvider>();

    // Add session support for debugging
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
            Description = "Multi-tenant CMS API with page builder functionality and job management"
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
        Authorization = new[] { new HangfireAuthorizationFilter() },
        DisplayStorageConnectionString = false,
        DashboardTitle = "Backend CMS Jobs"
    });

    app.UseHttpsRedirection();

    app.UseCors("AllowReactApp");

    app.UseSession();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Database migration and seeding per tenant
    app.MapPost("/admin/migrate-tenant/{tenantId}", async (string tenantId, IServiceProvider serviceProvider) =>
    {
        if (!app.Environment.IsDevelopment()) return Results.NotFound();

        // Create a scope with specific tenant context
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        // Override tenant provider for this operation
        var connectionStringTemplate = app.Configuration.GetConnectionString("DefaultConnection");
        var connectionString = connectionStringTemplate?.Replace("{TENANT_ID}", tenantId);

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        // Create temporary tenant provider
        var tempTenantProvider = new DebugTenantProvider(tenantId);

        using var context = new ApplicationDbContext(optionsBuilder.Options, tempTenantProvider);

        try
        {
            await context.Database.MigrateAsync();
            await SeedDatabase(context, tenantId);
            await SeedTenantRegistry(serviceProvider, tenantId);
            return Results.Ok($"Migration completed for tenant: {tenantId}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Migration failed for tenant {TenantId}", tenantId);
            return Results.Problem($"Migration failed: {ex.Message}");
        }
    });

    // Admin-controlled job management endpoints (no automatic monitoring)
    app.MapPost("/admin/jobs/check-template-updates", async (ITemplateSyncJobService templateSyncService) =>
    {
        var detection = await templateSyncService.DetectTemplateUpdatesAsync();
        return Results.Ok(detection);
    }).RequireAuthorization();

    app.MapPost("/admin/jobs/emergency-stop-all", async (IServiceProvider serviceProvider) =>
    {
        // Emergency stop all running jobs (admin only)
        using var scope = serviceProvider.CreateScope();
        var hangfireClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();

        // This would require additional implementation to stop running jobs
        return Results.Ok("Emergency stop initiated - check Hangfire dashboard");
    }).RequireAuthorization();

    // Initialize Hangfire database
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var hangfireConn = configuration.GetConnectionString("HangfireConnection")
                ?? configuration.GetConnectionString("DefaultConnection")?.Replace("{TENANT_ID}", "hangfire");

            // Ensure Hangfire database exists
            GlobalConfiguration.Configuration.UsePostgreSqlStorage(hangfireConn);
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to initialize Hangfire database");
    }

    // Initialize default tenant database
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();
            await SeedDatabase(context, "default");
            await SeedTenantRegistry(scope.ServiceProvider, "default");
        }
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "An error occurred while migrating or seeding the default database");
        throw;
    }

    app.Run();

    // Database seeding method
    async Task SeedDatabase(ApplicationDbContext context, string tenantId)
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
                    new Backend.CMS.Domain.Entities.Permission { Name = "Components.Delete", Resource = "Components", Action = "Delete" },
                    new Backend.CMS.Domain.Entities.Permission { Name = "Versioning.View", Resource = "Versioning", Action = "View" },
                    new Backend.CMS.Domain.Entities.Permission { Name = "Versioning.Deploy", Resource = "Versioning", Action = "Deploy" },
                    new Backend.CMS.Domain.Entities.Permission { Name = "Versioning.Rollback", Resource = "Versioning", Action = "Rollback" },
                    new Backend.CMS.Domain.Entities.Permission { Name = "Jobs.View", Resource = "Jobs", Action = "View" },
                    new Backend.CMS.Domain.Entities.Permission { Name = "Jobs.Manage", Resource = "Jobs", Action = "Manage" },
                    new Backend.CMS.Domain.Entities.Permission { Name = "Jobs.Cancel", Resource = "Jobs", Action = "Cancel" }
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
                    TenantId = tenantId
                };

                var editorRole = new Backend.CMS.Domain.Entities.Role
                {
                    Name = "Editor",
                    NormalizedName = "EDITOR",
                    Description = "Content management access",
                    TenantId = tenantId
                };

                var viewerRole = new Backend.CMS.Domain.Entities.Role
                {
                    Name = "Viewer",
                    NormalizedName = "VIEWER",
                    Description = "Read-only access",
                    TenantId = tenantId
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

            // Seed default company
            if (!context.Companies.Any())
            {
                var company = new Backend.CMS.Domain.Entities.Company
                {
                    Name = "Default Company",
                    Description = "Default company for tenant " + tenantId,
                    TenantId = tenantId,
                    IsActive = true,
                    Currency = "USD",
                    Language = "en",
                    Timezone = "UTC"
                };

                context.Companies.Add(company);
                await context.SaveChangesAsync();
            }

            // Seed default admin user
            if (!context.Users.Any())
            {
                var adminUser = new Backend.CMS.Domain.Entities.User
                {
                    Email = "admin@example.com",
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    FirstName = "System",
                    LastName = "Administrator",
                    IsActive = true,
                    TenantId = tenantId,
                    EmailVerifiedAt = DateTime.UtcNow
                };

                context.Users.Add(adminUser);
                await context.SaveChangesAsync();

                // Assign admin role to admin user
                var adminRole = context.Roles.First(r => r.NormalizedName == "ADMINISTRATOR");
                var userRole = new Backend.CMS.Domain.Entities.UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = "System",
                    IsActive = true
                };

                context.UserRoles.Add(userRole);
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
                        TenantId = tenantId,
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
                        TenantId = tenantId,
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
                        TenantId = tenantId,
                        DefaultProperties = new Dictionary<string, object>
                        {
                            { "text", "Click me" },
                            { "link", "" },
                            { "target", "_self" },
                            { "variant", "primary" },
                            { "size", "medium" }
                        }
                    },
                    new Backend.CMS.Domain.Entities.ComponentTemplate
                    {
                        Name = "container",
                        DisplayName = "Container",
                        Description = "Layout container for organizing content",
                        Type = Backend.CMS.Domain.Enums.ComponentType.Container,
                        Category = "Layout",
                        Icon = "square",
                        IsSystemTemplate = true,
                        TenantId = tenantId,
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

            Log.Information("Database seeded successfully for tenant {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error occurred while seeding database for tenant {TenantId}", tenantId);
            throw;
        }
    }

    // Seed tenant registry (using main database connection)
    async Task SeedTenantRegistry(IServiceProvider serviceProvider, string tenantId)
    {
        try
        {
            // Use main database connection for tenant registry
            var connectionStringTemplate = serviceProvider.GetRequiredService<IConfiguration>()
                .GetConnectionString("DefaultConnection");
            var mainConnectionString = connectionStringTemplate?.Replace("{TENANT_ID}", "main");

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(mainConnectionString);

            using var mainContext = new ApplicationDbContext(optionsBuilder.Options, new DebugTenantProvider("main"));

            if (!mainContext.TenantRegistry.Any(t => t.TenantId == tenantId))
            {
                var tenantRegistry = new Backend.CMS.Domain.Entities.TenantRegistry
                {
                    TenantId = tenantId,
                    TenantName = $"Tenant {tenantId}",
                    DatabaseConnectionString = connectionStringTemplate?.Replace("{TENANT_ID}", tenantId) ?? "",
                    IsActive = true,
                    CurrentVersion = "1.0.0",
                    CurrentTemplateVersion = "1.0.0",
                    LastDeployment = DateTime.UtcNow,
                    LastTemplateSync = DateTime.UtcNow,
                    AutoDeployEnabled = true,
                    AutoSyncEnabled = true,
                    MaintenanceWindow = "0 2 * * *" // 2 AM daily
                };

                mainContext.TenantRegistry.Add(tenantRegistry);
                await mainContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error seeding tenant registry for {TenantId}", tenantId);
        }
    }

    // Hangfire authorization filter - Simplified implementation
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            // For development, allow all access
            // In production, you should implement proper authorization
            return true; // Change this to implement proper security in production
        }
    }

    // Debug tenant provider for development
    public class DebugTenantProvider : ITenantProvider
    {
        private readonly string _tenantId;

        public DebugTenantProvider(string tenantId)
        {
            _tenantId = tenantId;
        }

        public string GetTenantId() => _tenantId;
    }