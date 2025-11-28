using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codewrinkles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixPulseImageRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PulseId1",
                schema: "pulse",
                table: "PulseImages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PulseImages_PulseId1",
                schema: "pulse",
                table: "PulseImages",
                column: "PulseId1",
                unique: true,
                filter: "[PulseId1] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_PulseImages_Pulses_PulseId1",
                schema: "pulse",
                table: "PulseImages",
                column: "PulseId1",
                principalSchema: "pulse",
                principalTable: "Pulses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PulseImages_Pulses_PulseId1",
                schema: "pulse",
                table: "PulseImages");

            migrationBuilder.DropIndex(
                name: "IX_PulseImages_PulseId1",
                schema: "pulse",
                table: "PulseImages");

            migrationBuilder.DropColumn(
                name: "PulseId1",
                schema: "pulse",
                table: "PulseImages");
        }
    }
}
