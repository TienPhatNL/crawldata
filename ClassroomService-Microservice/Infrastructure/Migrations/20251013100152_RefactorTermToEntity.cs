using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorTermToEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Courses_CourseCodeId_Term_Year_LecturerId",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_CourseRequests_CourseCodeId_Term_Year_LecturerId_Status",
                table: "CourseRequests");

            migrationBuilder.DropColumn(
                name: "Term",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Term",
                table: "CourseRequests");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "RejectionReason",
                table: "Courses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApprovalComments",
                table: "Courses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TermId",
                table: "Courses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TermId",
                table: "CourseRequests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Terms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Terms", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CourseCodeId_TermId_Year_LecturerId",
                table: "Courses",
                columns: new[] { "CourseCodeId", "TermId", "Year", "LecturerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_TermId",
                table: "Courses",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequests_CourseCodeId_TermId_Year_LecturerId_Status",
                table: "CourseRequests",
                columns: new[] { "CourseCodeId", "TermId", "Year", "LecturerId", "Status" },
                filter: "Status = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequests_TermId",
                table: "CourseRequests",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_Terms_IsActive",
                table: "Terms",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Terms_Name",
                table: "Terms",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseRequests_Terms_TermId",
                table: "CourseRequests",
                column: "TermId",
                principalTable: "Terms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Terms_TermId",
                table: "Courses",
                column: "TermId",
                principalTable: "Terms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseRequests_Terms_TermId",
                table: "CourseRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Terms_TermId",
                table: "Courses");

            migrationBuilder.DropTable(
                name: "Terms");

            migrationBuilder.DropIndex(
                name: "IX_Courses_CourseCodeId_TermId_Year_LecturerId",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_TermId",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_CourseRequests_CourseCodeId_TermId_Year_LecturerId_Status",
                table: "CourseRequests");

            migrationBuilder.DropIndex(
                name: "IX_CourseRequests_TermId",
                table: "CourseRequests");

            migrationBuilder.DropColumn(
                name: "TermId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "TermId",
                table: "CourseRequests");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Courses",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "RejectionReason",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApprovalComments",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Term",
                table: "Courses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Term",
                table: "CourseRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CourseCodeId_Term_Year_LecturerId",
                table: "Courses",
                columns: new[] { "CourseCodeId", "Term", "Year", "LecturerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequests_CourseCodeId_Term_Year_LecturerId_Status",
                table: "CourseRequests",
                columns: new[] { "CourseCodeId", "Term", "Year", "LecturerId", "Status" },
                filter: "Status = 1");
        }
    }
}
