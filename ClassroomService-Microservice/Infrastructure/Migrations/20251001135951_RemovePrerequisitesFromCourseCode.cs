using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePrerequisitesFromCourseCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Courses_CourseCodeId_Term_Year_Section",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Section",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CreditHours",
                table: "CourseCodes");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "CourseCodes");

            migrationBuilder.DropColumn(
                name: "Prerequisites",
                table: "CourseCodes");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CourseCodeId_Term_Year_LecturerId",
                table: "Courses",
                columns: new[] { "CourseCodeId", "Term", "Year", "LecturerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Courses_CourseCodeId_Term_Year_LecturerId",
                table: "Courses");

            migrationBuilder.AddColumn<string>(
                name: "Section",
                table: "Courses",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreditHours",
                table: "CourseCodes",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "CourseCodes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Prerequisites",
                table: "CourseCodes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CourseCodeId_Term_Year_Section",
                table: "Courses",
                columns: new[] { "CourseCodeId", "Term", "Year", "Section" },
                unique: true,
                filter: "[Section] IS NOT NULL");
        }
    }
}
