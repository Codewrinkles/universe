using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codewrinkles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeProfileHandleNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Profiles_Handle",
                schema: "identity",
                table: "Profiles");

            migrationBuilder.AlterColumn<string>(
                name: "Handle",
                schema: "identity",
                table: "Profiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_Handle",
                schema: "identity",
                table: "Profiles",
                column: "Handle",
                unique: true,
                filter: "[Handle] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Profiles_Handle",
                schema: "identity",
                table: "Profiles");

            migrationBuilder.AlterColumn<string>(
                name: "Handle",
                schema: "identity",
                table: "Profiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_Handle",
                schema: "identity",
                table: "Profiles",
                column: "Handle",
                unique: true);
        }
    }
}
