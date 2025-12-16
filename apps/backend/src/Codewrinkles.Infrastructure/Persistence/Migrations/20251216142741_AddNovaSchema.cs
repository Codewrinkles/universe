using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codewrinkles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNovaSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "nova");

            migrationBuilder.CreateTable(
                name: "ConversationSessions",
                schema: "nova",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    LastMessageAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationSessions_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalSchema: "identity",
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                schema: "nova",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<byte>(type: "tinyint", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 100000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    TokensUsed = table.Column<int>(type: "int", nullable: true),
                    ModelUsed = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_ConversationSessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "nova",
                        principalTable: "ConversationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationSessions_IsDeleted",
                schema: "nova",
                table: "ConversationSessions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationSessions_ProfileId",
                schema: "nova",
                table: "ConversationSessions",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationSessions_ProfileId_IsDeleted_LastMessageAt",
                schema: "nova",
                table: "ConversationSessions",
                columns: new[] { "ProfileId", "IsDeleted", "LastMessageAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SessionId_CreatedAt",
                schema: "nova",
                table: "Messages",
                columns: new[] { "SessionId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Messages",
                schema: "nova");

            migrationBuilder.DropTable(
                name: "ConversationSessions",
                schema: "nova");
        }
    }
}
