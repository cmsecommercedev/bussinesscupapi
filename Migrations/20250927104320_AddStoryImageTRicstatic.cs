using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinessCupApi.Migrations
{
    /// <inheritdoc />
    public partial class AddStoryImageTRicstatic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrl",
                table: "RichStaticContents",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileImageUrl",
                table: "RichStaticContents");
        }
    }
}
