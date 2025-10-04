using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BussinessCupApi.Migrations
{
    /// <inheritdoc />
    public partial class AddStoryImageTRdfgdfgggdfg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeasonId",
                table: "RichStaticContents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RichStaticContents_SeasonId",
                table: "RichStaticContents",
                column: "SeasonId");

            migrationBuilder.AddForeignKey(
                name: "FK_RichStaticContents_Season_SeasonId",
                table: "RichStaticContents",
                column: "SeasonId",
                principalTable: "Season",
                principalColumn: "SeasonID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RichStaticContents_Season_SeasonId",
                table: "RichStaticContents");

            migrationBuilder.DropIndex(
                name: "IX_RichStaticContents_SeasonId",
                table: "RichStaticContents");

            migrationBuilder.DropColumn(
                name: "SeasonId",
                table: "RichStaticContents");
        }
    }
}
