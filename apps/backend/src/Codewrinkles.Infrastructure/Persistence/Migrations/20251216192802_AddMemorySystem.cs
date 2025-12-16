using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codewrinkles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMemorySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastMemoryExtractionAt",
                schema: "nova",
                table: "ConversationSessions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastProcessedMessageId",
                schema: "nova",
                table: "ConversationSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Memories",
                schema: "nova",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Embedding = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Importance = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    OccurrenceCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    SupersededAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SupersededById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Memories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Memories_ConversationSessions_SourceSessionId",
                        column: x => x.SourceSessionId,
                        principalSchema: "nova",
                        principalTable: "ConversationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Memories_Memories_SupersededById",
                        column: x => x.SupersededById,
                        principalSchema: "nova",
                        principalTable: "Memories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Memories_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalSchema: "identity",
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationSessions_ProfileId_LastMemoryExtractionAt",
                schema: "nova",
                table: "ConversationSessions",
                columns: new[] { "ProfileId", "LastMemoryExtractionAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Memories_ProfileId_Category_SupersededAt",
                schema: "nova",
                table: "Memories",
                columns: new[] { "ProfileId", "Category", "SupersededAt" },
                filter: "[SupersededAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Memories_ProfileId_CreatedAt_Desc",
                schema: "nova",
                table: "Memories",
                columns: new[] { "ProfileId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Memories_ProfileId_Importance_Desc",
                schema: "nova",
                table: "Memories",
                columns: new[] { "ProfileId", "Importance" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Memories_SourceSessionId",
                schema: "nova",
                table: "Memories",
                column: "SourceSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Memories_SupersededById",
                schema: "nova",
                table: "Memories",
                column: "SupersededById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Memories",
                schema: "nova");

            migrationBuilder.DropIndex(
                name: "IX_ConversationSessions_ProfileId_LastMemoryExtractionAt",
                schema: "nova",
                table: "ConversationSessions");

            migrationBuilder.DropColumn(
                name: "LastMemoryExtractionAt",
                schema: "nova",
                table: "ConversationSessions");

            migrationBuilder.DropColumn(
                name: "LastProcessedMessageId",
                schema: "nova",
                table: "ConversationSessions");
        }
    }
}
