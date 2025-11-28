using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codewrinkles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPulseMentions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PulseMentions",
                schema: "pulse",
                columns: table => new
                {
                    PulseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Handle = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PulseMentions", x => new { x.PulseId, x.ProfileId });
                    table.ForeignKey(
                        name: "FK_PulseMentions_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalSchema: "identity",
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PulseMentions_Pulses_PulseId",
                        column: x => x.PulseId,
                        principalSchema: "pulse",
                        principalTable: "Pulses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PulseMentions_CreatedAt",
                schema: "pulse",
                table: "PulseMentions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PulseMentions_Handle",
                schema: "pulse",
                table: "PulseMentions",
                column: "Handle");

            migrationBuilder.CreateIndex(
                name: "IX_PulseMentions_ProfileId",
                schema: "pulse",
                table: "PulseMentions",
                column: "ProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PulseMentions",
                schema: "pulse");
        }
    }
}
