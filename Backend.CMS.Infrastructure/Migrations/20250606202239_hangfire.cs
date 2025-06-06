using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.CMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class hangfire : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeploymentJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReleaseNotes = table.Column<string>(type: "text", nullable: false),
                    MigrationData = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ScheduledBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExecutedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    JobMetadata = table.Column<string>(type: "text", nullable: false),
                    IsGlobalJob = table.Column<bool>(type: "boolean", nullable: false),
                    TotalTenants = table.Column<int>(type: "integer", nullable: false),
                    CompletedTenants = table.Column<int>(type: "integer", nullable: false),
                    FailedTenants = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeploymentProposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReleaseNotes = table.Column<string>(type: "text", nullable: false),
                    MigrationData = table.Column<string>(type: "text", nullable: false),
                    ProposedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProposedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNotes = table.Column<string>(type: "text", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    AffectedTenants = table.Column<string>(type: "text", nullable: false),
                    ImpactAnalysis = table.Column<string>(type: "text", nullable: false),
                    RequiresMaintenanceWindow = table.Column<bool>(type: "boolean", nullable: false),
                    EstimatedDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    RiskLevel = table.Column<string>(type: "text", nullable: false),
                    PrerequisiteChecks = table.Column<string>(type: "text", nullable: false),
                    RollbackPlan = table.Column<string>(type: "text", nullable: false),
                    HasBreakingChanges = table.Column<bool>(type: "boolean", nullable: false),
                    ScheduledDeploymentTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedJobId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentProposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TemplateSyncJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MasterTemplateVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PreviousVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ScheduledBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExecutedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    JobMetadata = table.Column<string>(type: "text", nullable: false),
                    IsGlobalJob = table.Column<bool>(type: "boolean", nullable: false),
                    TotalTenants = table.Column<int>(type: "integer", nullable: false),
                    CompletedTenants = table.Column<int>(type: "integer", nullable: false),
                    FailedTenants = table.Column<string>(type: "text", nullable: false),
                    RequiresManualReview = table.Column<bool>(type: "boolean", nullable: false),
                    ConflictResolutions = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateSyncJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TemplateUpdateProposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MasterTemplateVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PreviousVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DetectedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNotes = table.Column<string>(type: "text", nullable: true),
                    ChangedFiles = table.Column<string>(type: "text", nullable: false),
                    AddedFiles = table.Column<string>(type: "text", nullable: false),
                    DeletedFiles = table.Column<string>(type: "text", nullable: false),
                    BreakingChanges = table.Column<string>(type: "text", nullable: false),
                    ConflictAnalysis = table.Column<string>(type: "text", nullable: false),
                    RequiresManualReview = table.Column<bool>(type: "boolean", nullable: false),
                    RiskLevel = table.Column<string>(type: "text", nullable: false),
                    AffectedTenants = table.Column<string>(type: "text", nullable: false),
                    ImpactAnalysis = table.Column<string>(type: "text", nullable: false),
                    ScheduledSyncTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedJobId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateUpdateProposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantRegistry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DatabaseConnectionString = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CurrentTemplateVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TenantMetadata = table.Column<string>(type: "text", nullable: false),
                    LastDeployment = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastTemplateSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AutoDeployEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AutoSyncEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MaintenanceWindow = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantRegistry", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentJobs_JobId",
                table: "DeploymentJobs",
                column: "JobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSyncJobs_JobId",
                table: "TemplateSyncJobs",
                column: "JobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantRegistry_TenantId",
                table: "TenantRegistry",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeploymentJobs");

            migrationBuilder.DropTable(
                name: "DeploymentProposals");

            migrationBuilder.DropTable(
                name: "TemplateSyncJobs");

            migrationBuilder.DropTable(
                name: "TemplateUpdateProposals");

            migrationBuilder.DropTable(
                name: "TenantRegistry");
        }
    }
}
