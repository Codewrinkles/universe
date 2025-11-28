using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codewrinkles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationAndWebsiteUrlToProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Location",
                schema: "identity",
                table: "Profiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebsiteUrl",
                schema: "identity",
                table: "Profiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                schema: "identity",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "WebsiteUrl",
                schema: "identity",
                table: "Profiles");
        }
    }
}
