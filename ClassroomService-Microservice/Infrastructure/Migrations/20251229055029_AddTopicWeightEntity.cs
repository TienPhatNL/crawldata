using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicWeightEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TopicWeights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SpecificCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WeightPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConfiguredBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicWeights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopicWeights_CourseCodes_CourseCodeId",
                        column: x => x.CourseCodeId,
                        principalTable: "CourseCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TopicWeights_Courses_SpecificCourseId",
                        column: x => x.SpecificCourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TopicWeights_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TopicWeights_ConfiguredBy",
                table: "TopicWeights",
                column: "ConfiguredBy");

            migrationBuilder.CreateIndex(
                name: "IX_TopicWeights_CourseCodeId",
                table: "TopicWeights",
                column: "CourseCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicWeights_SpecificCourseId",
                table: "TopicWeights",
                column: "SpecificCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicWeights_TopicId_CourseCodeId",
                table: "TopicWeights",
                columns: new[] { "TopicId", "CourseCodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_TopicWeights_TopicId_SpecificCourseId",
                table: "TopicWeights",
                columns: new[] { "TopicId", "SpecificCourseId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TopicWeights");
        }
    }
}
