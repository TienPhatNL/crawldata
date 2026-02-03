using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceEnrollmentTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "CourseEnrollments",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnenrolledAt",
                table: "CourseEnrollments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UnenrolledBy",
                table: "CourseEnrollments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnenrollmentReason",
                table: "CourseEnrollments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_JoinedAt",
                table: "CourseEnrollments",
                column: "JoinedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_Status",
                table: "CourseEnrollments",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_JoinedAt",
                table: "CourseEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_Status",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "UnenrolledAt",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "UnenrolledBy",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "UnenrollmentReason",
                table: "CourseEnrollments");
        }
    }
}
