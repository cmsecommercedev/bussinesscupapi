using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinessCupApi.Migrations
{
    /// <inheritdoc />
    public partial class AddStoryImageTRdfgdfg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FavouriteTeams_Teams_TeamID",
                table: "FavouriteTeams");

            migrationBuilder.DropIndex(
                name: "IX_FavouriteTeams_TeamID",
                table: "FavouriteTeams");

            migrationBuilder.RenameColumn(
                name: "VideoUrl",
                table: "RichStaticContents",
                newName: "MediaUrl");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "RichStaticContents",
                newName: "EmbedVideoUrl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MediaUrl",
                table: "RichStaticContents",
                newName: "VideoUrl");

            migrationBuilder.RenameColumn(
                name: "EmbedVideoUrl",
                table: "RichStaticContents",
                newName: "ImageUrl");

            migrationBuilder.CreateIndex(
                name: "IX_FavouriteTeams_TeamID",
                table: "FavouriteTeams",
                column: "TeamID");

            migrationBuilder.AddForeignKey(
                name: "FK_FavouriteTeams_Teams_TeamID",
                table: "FavouriteTeams",
                column: "TeamID",
                principalTable: "Teams",
                principalColumn: "TeamID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
