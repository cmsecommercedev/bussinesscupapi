using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinessCupApi.Migrations
{
    /// <inheritdoc />
    public partial class AddStoryImage555df : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryCode",
                table: "RichStaticContents");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "RichStaticContents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RichContentCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RichContentCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RichStaticContents_CategoryId",
                table: "RichStaticContents",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_RichStaticContents_RichContentCategories_CategoryId",
                table: "RichStaticContents",
                column: "CategoryId",
                principalTable: "RichContentCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RichStaticContents_RichContentCategories_CategoryId",
                table: "RichStaticContents");

            migrationBuilder.DropTable(
                name: "RichContentCategories");

            migrationBuilder.DropIndex(
                name: "IX_RichStaticContents_CategoryId",
                table: "RichStaticContents");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "RichStaticContents");

            migrationBuilder.AddColumn<string>(
                name: "CategoryCode",
                table: "RichStaticContents",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
