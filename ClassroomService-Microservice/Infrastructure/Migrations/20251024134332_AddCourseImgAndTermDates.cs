using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseImgAndTermDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupMembers_CourseEnrollments_EnrollmentId",
                table: "GroupMembers");

            // Add StartDate and EndDate columns as nullable first
            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Terms",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Terms",
                type: "datetime2",
                nullable: true);

            // Set default dates for existing terms (if any)
            // You may want to adjust these dates based on your actual data
            migrationBuilder.Sql(@"
                UPDATE Terms 
                SET StartDate = DATEADD(MONTH, -6, GETUTCDATE()),
                    EndDate = DATEADD(MONTH, 6, GETUTCDATE())
                WHERE StartDate IS NULL OR EndDate IS NULL;
            ");

            // Make the columns non-nullable
            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Terms",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "Terms",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            // Add Img column to Courses (changed max length to 500)
            migrationBuilder.AddColumn<string>(
                name: "Img",
                table: "Courses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // Create index for date range queries
            migrationBuilder.CreateIndex(
                name: "IX_Terms_StartDate_EndDate",
                table: "Terms",
                columns: new[] { "StartDate", "EndDate" });

            // Re-add FK with NoAction to avoid cascade path conflicts
            migrationBuilder.AddForeignKey(
                name: "FK_GroupMembers_CourseEnrollments_EnrollmentId",
                table: "GroupMembers",
                column: "EnrollmentId",
                principalTable: "CourseEnrollments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupMembers_CourseEnrollments_EnrollmentId",
                table: "GroupMembers");

            migrationBuilder.DropIndex(
                name: "IX_Terms_StartDate_EndDate",
                table: "Terms");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Terms");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Terms");

            migrationBuilder.DropColumn(
                name: "Img",
                table: "Courses");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupMembers_CourseEnrollments_EnrollmentId",
                table: "GroupMembers",
                column: "EnrollmentId",
                principalTable: "CourseEnrollments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
