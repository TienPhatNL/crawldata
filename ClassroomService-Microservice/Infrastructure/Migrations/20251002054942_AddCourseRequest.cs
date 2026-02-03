using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Term = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LecturerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    RequestReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProcessedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessingComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseRequests_CourseCodes_CourseCodeId",
                        column: x => x.CourseCodeId,
                        principalTable: "CourseCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseRequests_Courses_CreatedCourseId",
                        column: x => x.CreatedCourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequests_CourseCodeId",
                table: "CourseRequests",
                column: "CourseCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequests_CourseCodeId_Term_Year_LecturerId_Status",
                table: "CourseRequests",
                columns: new[] { "CourseCodeId", "Term", "Year", "LecturerId", "Status" },
                filter: "Status = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequests_CreatedCourseId",
                table: "CourseRequests",
                column: "CreatedCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequests_LecturerId",
                table: "CourseRequests",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequests_ProcessedBy",
                table: "CourseRequests",
                column: "ProcessedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequests_Status",
                table: "CourseRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseRequests");
        }
    }
}
