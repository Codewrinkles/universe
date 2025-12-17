using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codewrinkles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNovaAccessToProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFoundingMember",
                schema: "identity",
                table: "Profiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "NovaAccess",
                schema: "identity",
                table: "Profiles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFoundingMember",
                schema: "identity",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "NovaAccess",
                schema: "identity",
                table: "Profiles");
        }
    }
}
