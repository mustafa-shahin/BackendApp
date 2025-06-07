using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.CMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RegisterFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PageComponents_PageComponents_ParentComponentId",
                table: "PageComponents");

            migrationBuilder.DropForeignKey(
                name: "FK_Pages_Pages_ParentPageId",
                table: "Pages");

            migrationBuilder.AddForeignKey(
                name: "FK_PageComponents_PageComponents_ParentComponentId",
                table: "PageComponents",
                column: "ParentComponentId",
                principalTable: "PageComponents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pages_Pages_ParentPageId",
                table: "Pages",
                column: "ParentPageId",
                principalTable: "Pages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PageComponents_PageComponents_ParentComponentId",
                table: "PageComponents");

            migrationBuilder.DropForeignKey(
                name: "FK_Pages_Pages_ParentPageId",
                table: "Pages");

            migrationBuilder.AddForeignKey(
                name: "FK_PageComponents_PageComponents_ParentComponentId",
                table: "PageComponents",
                column: "ParentComponentId",
                principalTable: "PageComponents",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Pages_Pages_ParentPageId",
                table: "Pages",
                column: "ParentPageId",
                principalTable: "Pages",
                principalColumn: "Id");
        }
    }
}
