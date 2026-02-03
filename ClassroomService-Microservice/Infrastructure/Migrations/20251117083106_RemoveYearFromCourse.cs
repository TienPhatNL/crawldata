using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveYearFromCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Courses_CourseCodeId_TermId_Year_LecturerId_UniqueCode",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "Courses");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CourseCodeId_TermId_LecturerId_UniqueCode",
                table: "Courses",
                columns: new[] { "CourseCodeId", "TermId", "LecturerId", "UniqueCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Courses_CourseCodeId_TermId_LecturerId_UniqueCode",
                table: "Courses");

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CourseCodeId_TermId_Year_LecturerId_UniqueCode",
                table: "Courses",
                columns: new[] { "CourseCodeId", "TermId", "Year", "LecturerId", "UniqueCode" },
                unique: true);
        }
    }
}
