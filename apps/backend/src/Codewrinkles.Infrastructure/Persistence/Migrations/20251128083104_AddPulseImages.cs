using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codewrinkles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPulseImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PulseImages",
                schema: "pulse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PulseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PulseImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PulseImages_Pulses_PulseId",
                        column: x => x.PulseId,
                        principalSchema: "pulse",
                        principalTable: "Pulses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PulseImages_PulseId",
                schema: "pulse",
                table: "PulseImages",
                column: "PulseId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PulseImages",
                schema: "pulse");
        }
    }
}
