using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupsAndGroupMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignmentId",
                table: "Groups",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Groups",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "Groups",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxMembers",
                table: "Groups",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGroupAssignment",
                table: "Assignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "GroupMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsLeader = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Role = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupMembers_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Groups_AssignmentId",
                table: "Groups",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_CourseId_Name",
                table: "Groups",
                columns: new[] { "CourseId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Groups_IsLocked",
                table: "Groups",
                column: "IsLocked");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_IsGroupAssignment",
                table: "Assignments",
                column: "IsGroupAssignment");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_GroupId_StudentId",
                table: "GroupMembers",
                columns: new[] { "GroupId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_IsLeader",
                table: "GroupMembers",
                column: "IsLeader");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_JoinedAt",
                table: "GroupMembers",
                column: "JoinedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_Role",
                table: "GroupMembers",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_StudentId",
                table: "GroupMembers",
                column: "StudentId");

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

            migrationBuilder.DropTable(
                name: "GroupMembers");

            migrationBuilder.DropIndex(
                name: "IX_Groups_AssignmentId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_CourseId_Name",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_IsLocked",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_IsGroupAssignment",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "AssignmentId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "MaxMembers",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "IsGroupAssignment",
                table: "Assignments");
        }
    }
}
