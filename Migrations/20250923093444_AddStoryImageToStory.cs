using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinessCupApi.Migrations
{
    /// <inheritdoc />
    public partial class AddStoryImageToStory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StoryImage",
                table: "Stories",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StoryImage",
                table: "Stories");
        }
    }
}
