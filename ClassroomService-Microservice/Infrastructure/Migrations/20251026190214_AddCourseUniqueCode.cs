using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseUniqueCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Courses_CourseCodeId_TermId_Year_LecturerId",
                table: "Courses");

            migrationBuilder.AddColumn<string>(
                name: "UniqueCode",
                table: "Courses",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CourseCodeId_TermId_Year_LecturerId_UniqueCode",
                table: "Courses",
                columns: new[] { "CourseCodeId", "TermId", "Year", "LecturerId", "UniqueCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_UniqueCode",
                table: "Courses",
                column: "UniqueCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Courses_CourseCodeId_TermId_Year_LecturerId_UniqueCode",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_UniqueCode",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "UniqueCode",
                table: "Courses");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CourseCodeId_TermId_Year_LecturerId",
                table: "Courses",
                columns: new[] { "CourseCodeId", "TermId", "Year", "LecturerId" },
                unique: true);
        }
    }
}
