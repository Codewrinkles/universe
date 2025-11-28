using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codewrinkles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OptimizePulseQueryPerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Pulses_AuthorId_IsDeleted_CreatedAt",
                schema: "pulse",
                table: "Pulses",
                columns: new[] { "AuthorId", "IsDeleted", "CreatedAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Pulses_IsDeleted_CreatedAt",
                schema: "pulse",
                table: "Pulses",
                columns: new[] { "IsDeleted", "CreatedAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pulses_AuthorId_IsDeleted_CreatedAt",
                schema: "pulse",
                table: "Pulses");

            migrationBuilder.DropIndex(
                name: "IX_Pulses_IsDeleted_CreatedAt",
                schema: "pulse",
                table: "Pulses");
        }
    }
}
