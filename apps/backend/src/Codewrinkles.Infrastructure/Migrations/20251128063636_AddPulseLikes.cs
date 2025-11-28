using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codewrinkles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPulseLikes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PulseLikes",
                schema: "pulse",
                columns: table => new
                {
                    PulseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PulseLikes", x => new { x.PulseId, x.ProfileId });
                    table.ForeignKey(
                        name: "FK_PulseLikes_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalSchema: "identity",
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PulseLikes_Pulses_PulseId",
                        column: x => x.PulseId,
                        principalSchema: "pulse",
                        principalTable: "Pulses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PulseLikes_CreatedAt",
                schema: "pulse",
                table: "PulseLikes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PulseLikes_ProfileId",
                schema: "pulse",
                table: "PulseLikes",
                column: "ProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PulseLikes",
                schema: "pulse");
        }
    }
}
