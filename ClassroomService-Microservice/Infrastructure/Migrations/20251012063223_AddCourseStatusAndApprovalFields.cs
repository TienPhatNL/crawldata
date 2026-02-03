using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseStatusAndApprovalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Assignments_AssignmentId",
                table: "Groups");

            migrationBuilder.AddColumn<string>(
                name: "ApprovalComments",
                table: "Courses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Courses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedBy",
                table: "Courses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Courses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_ApprovedBy",
                table: "Courses",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Status",
                table: "Courses",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Assignments_AssignmentId",
                table: "Groups",
                column: "AssignmentId",
                principalTable: "Assignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Assignments_AssignmentId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Courses_ApprovedBy",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_Status",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "ApprovalComments",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Courses");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Assignments_AssignmentId",
                table: "Groups",
                column: "AssignmentId",
                principalTable: "Assignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
