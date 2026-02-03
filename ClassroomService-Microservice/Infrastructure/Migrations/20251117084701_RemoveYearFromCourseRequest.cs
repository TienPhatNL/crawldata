using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveYearFromCourseRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CourseRequests_CourseCodeId_TermId_Year_LecturerId_Status",
                table: "CourseRequests");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "CourseRequests");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequests_CourseCodeId_TermId_LecturerId_Status",
                table: "CourseRequests",
                columns: new[] { "CourseCodeId", "TermId", "LecturerId", "Status" },
                filter: "Status = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CourseRequests_CourseCodeId_TermId_LecturerId_Status",
                table: "CourseRequests");

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "CourseRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequests_CourseCodeId_TermId_Year_LecturerId_Status",
                table: "CourseRequests",
                columns: new[] { "CourseCodeId", "TermId", "Year", "LecturerId", "Status" },
                filter: "Status = 1");
        }
    }
}
