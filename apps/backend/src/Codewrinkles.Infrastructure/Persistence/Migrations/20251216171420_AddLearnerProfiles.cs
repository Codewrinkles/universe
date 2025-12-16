using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codewrinkles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearnerProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LearnerProfiles",
                schema: "nova",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentRole = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExperienceYears = table.Column<int>(type: "int", nullable: true),
                    PrimaryTechStack = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CurrentProject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LearningGoals = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LearningStyle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PreferredPace = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IdentifiedStrengths = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IdentifiedStruggles = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearnerProfiles_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalSchema: "identity",
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LearnerProfiles_ProfileId",
                schema: "nova",
                table: "LearnerProfiles",
                column: "ProfileId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LearnerProfiles",
                schema: "nova");
        }
    }
}
