using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinessCupApi.Migrations
{
    /// <inheritdoc />
    public partial class AddStoryImage555dfsdfsdfrrdfdsd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Culture",
                table: "FavouriteTeams",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Culture",
                table: "FavouritePlayers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Culture",
                table: "FavouriteTeams");

            migrationBuilder.DropColumn(
                name: "Culture",
                table: "FavouritePlayers");
        }
    }
}
