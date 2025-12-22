using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codewrinkles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContentIngestionForRAG : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContentChunks",
                schema: "nova",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceIdentifier = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    Embedding = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    TokenCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Author = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Technology = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ChunkIndex = table.Column<int>(type: "int", nullable: true),
                    ParentDocumentId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    SectionPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentChunks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentIngestionJobs",
                schema: "nova",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ParentDocumentId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Author = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Technology = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SourceUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MaxPages = table.Column<int>(type: "int", nullable: true),
                    ChunksCreated = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalPages = table.Column<int>(type: "int", nullable: true),
                    PagesProcessed = table.Column<int>(type: "int", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentIngestionJobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentChunks_Author",
                schema: "nova",
                table: "ContentChunks",
                column: "Author");

            migrationBuilder.CreateIndex(
                name: "IX_ContentChunks_ParentDocumentId",
                schema: "nova",
                table: "ContentChunks",
                column: "ParentDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentChunks_Source",
                schema: "nova",
                table: "ContentChunks",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_ContentChunks_Source_SourceIdentifier",
                schema: "nova",
                table: "ContentChunks",
                columns: new[] { "Source", "SourceIdentifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentChunks_Technology",
                schema: "nova",
                table: "ContentChunks",
                column: "Technology");

            migrationBuilder.CreateIndex(
                name: "IX_ContentIngestionJobs_CreatedAt",
                schema: "nova",
                table: "ContentIngestionJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentIngestionJobs_ParentDocumentId",
                schema: "nova",
                table: "ContentIngestionJobs",
                column: "ParentDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentIngestionJobs_Status",
                schema: "nova",
                table: "ContentIngestionJobs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentChunks",
                schema: "nova");

            migrationBuilder.DropTable(
                name: "ContentIngestionJobs",
                schema: "nova");
        }
    }
}
