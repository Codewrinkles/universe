using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codewrinkles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddThreadRootIdToPulse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ThreadRootId",
                schema: "pulse",
                table: "Pulses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pulses_ThreadRootId",
                schema: "pulse",
                table: "Pulses",
                column: "ThreadRootId",
                filter: "[ThreadRootId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Pulses_Pulses_ThreadRootId",
                schema: "pulse",
                table: "Pulses",
                column: "ThreadRootId",
                principalSchema: "pulse",
                principalTable: "Pulses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pulses_Pulses_ThreadRootId",
                schema: "pulse",
                table: "Pulses");

            migrationBuilder.DropIndex(
                name: "IX_Pulses_ThreadRootId",
                schema: "pulse",
                table: "Pulses");

            migrationBuilder.DropColumn(
                name: "ThreadRootId",
                schema: "pulse",
                table: "Pulses");
        }
    }
}
