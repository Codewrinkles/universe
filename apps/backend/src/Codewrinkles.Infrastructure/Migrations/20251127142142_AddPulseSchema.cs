using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codewrinkles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPulseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "pulse");

            migrationBuilder.CreateTable(
                name: "Pulses",
                schema: "pulse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    RepulsedPulseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParentPulseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pulses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pulses_Profiles_AuthorId",
                        column: x => x.AuthorId,
                        principalSchema: "identity",
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pulses_Pulses_ParentPulseId",
                        column: x => x.ParentPulseId,
                        principalSchema: "pulse",
                        principalTable: "Pulses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pulses_Pulses_RepulsedPulseId",
                        column: x => x.RepulsedPulseId,
                        principalSchema: "pulse",
                        principalTable: "Pulses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PulseEngagements",
                schema: "pulse",
                columns: table => new
                {
                    PulseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReplyCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    RepulseCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LikeCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ViewCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PulseEngagements", x => x.PulseId);
                    table.ForeignKey(
                        name: "FK_PulseEngagements_Pulses_PulseId",
                        column: x => x.PulseId,
                        principalSchema: "pulse",
                        principalTable: "Pulses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pulses_AuthorId",
                schema: "pulse",
                table: "Pulses",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Pulses_CreatedAt",
                schema: "pulse",
                table: "Pulses",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Pulses_IsDeleted",
                schema: "pulse",
                table: "Pulses",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Pulses_ParentPulseId",
                schema: "pulse",
                table: "Pulses",
                column: "ParentPulseId",
                filter: "[ParentPulseId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Pulses_RepulsedPulseId",
                schema: "pulse",
                table: "Pulses",
                column: "RepulsedPulseId",
                filter: "[RepulsedPulseId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PulseEngagements",
                schema: "pulse");

            migrationBuilder.DropTable(
                name: "Pulses",
                schema: "pulse");
        }
    }
}
