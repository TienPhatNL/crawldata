using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicWeightHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TopicWeightHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TopicWeightId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SpecificCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TermId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TermName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OldWeightPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    NewWeightPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AffectedTerms = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicWeightHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopicWeightHistories_CourseCodes_CourseCodeId",
                        column: x => x.CourseCodeId,
                        principalTable: "CourseCodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TopicWeightHistories_Courses_SpecificCourseId",
                        column: x => x.SpecificCourseId,
                        principalTable: "Courses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TopicWeightHistories_Terms_TermId",
                        column: x => x.TermId,
                        principalTable: "Terms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TopicWeightHistories_TopicWeights_TopicWeightId",
                        column: x => x.TopicWeightId,
                        principalTable: "TopicWeights",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TopicWeightHistories_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TopicWeightHistories_Action",
                table: "TopicWeightHistories",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_TopicWeightHistories_CourseCodeId",
                table: "TopicWeightHistories",
                column: "CourseCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicWeightHistories_ModifiedAt",
                table: "TopicWeightHistories",
                column: "ModifiedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TopicWeightHistories_SpecificCourseId",
                table: "TopicWeightHistories",
                column: "SpecificCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicWeightHistories_TermId",
                table: "TopicWeightHistories",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicWeightHistories_TopicId",
                table: "TopicWeightHistories",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicWeightHistories_TopicWeightId",
                table: "TopicWeightHistories",
                column: "TopicWeightId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TopicWeightHistories");
        }
    }
}
