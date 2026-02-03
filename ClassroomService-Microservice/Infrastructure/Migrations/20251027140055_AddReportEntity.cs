using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReportEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubmittedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Submission = table.Column<string>(type: "nvarchar(max)", maxLength: 50000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Grade = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Feedback = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    GradedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GradedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsGroupSubmission = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reports_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Reports_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_AssignmentId",
                table: "Reports",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_AssignmentId_GroupId",
                table: "Reports",
                columns: new[] { "AssignmentId", "GroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_AssignmentId_SubmittedBy",
                table: "Reports",
                columns: new[] { "AssignmentId", "SubmittedBy" });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_GradedBy",
                table: "Reports",
                column: "GradedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_GroupId",
                table: "Reports",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Status",
                table: "Reports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_SubmittedAt",
                table: "Reports",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_SubmittedBy",
                table: "Reports",
                column: "SubmittedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reports");
        }
    }
}
