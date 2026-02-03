using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseAccessCodeFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessCode",
                table: "Courses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccessCodeAttempts",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "AccessCodeCreatedAt",
                table: "Courses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AccessCodeExpiresAt",
                table: "Courses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAccessCodeAttempt",
                table: "Courses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresAccessCode",
                table: "Courses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_AccessCode",
                table: "Courses",
                column: "AccessCode",
                filter: "AccessCode IS NOT NULL AND RequiresAccessCode = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_RequiresAccessCode",
                table: "Courses",
                column: "RequiresAccessCode",
                filter: "RequiresAccessCode = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Courses_AccessCode",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_RequiresAccessCode",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "AccessCode",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "AccessCodeAttempts",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "AccessCodeCreatedAt",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "AccessCodeExpiresAt",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "LastAccessCodeAttempt",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "RequiresAccessCode",
                table: "Courses");
        }
    }
}
