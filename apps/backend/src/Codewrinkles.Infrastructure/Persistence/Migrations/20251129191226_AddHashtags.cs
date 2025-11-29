using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codewrinkles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHashtags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Hashtags",
                schema: "pulse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TagDisplay = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PulseCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hashtags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PulseHashtags",
                schema: "pulse",
                columns: table => new
                {
                    PulseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HashtagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PulseHashtags", x => new { x.PulseId, x.HashtagId });
                    table.ForeignKey(
                        name: "FK_PulseHashtags_Hashtags_HashtagId",
                        column: x => x.HashtagId,
                        principalSchema: "pulse",
                        principalTable: "Hashtags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PulseHashtags_Pulses_PulseId",
                        column: x => x.PulseId,
                        principalSchema: "pulse",
                        principalTable: "Pulses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Hashtags_PulseCount_LastUsedAt",
                schema: "pulse",
                table: "Hashtags",
                columns: new[] { "PulseCount", "LastUsedAt" });

            migrationBuilder.CreateIndex(
                name: "UQ_Hashtags_Tag",
                schema: "pulse",
                table: "Hashtags",
                column: "Tag",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PulseHashtags_HashtagId",
                schema: "pulse",
                table: "PulseHashtags",
                column: "HashtagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PulseHashtags",
                schema: "pulse");

            migrationBuilder.DropTable(
                name: "Hashtags",
                schema: "pulse");
        }
    }
}
