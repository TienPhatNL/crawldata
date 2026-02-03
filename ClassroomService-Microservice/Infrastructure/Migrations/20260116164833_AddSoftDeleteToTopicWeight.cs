using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteToTopicWeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "TopicWeights",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "TopicWeights",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TopicWeights",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_TopicWeights_IsDeleted",
                table: "TopicWeights",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TopicWeights_IsDeleted",
                table: "TopicWeights");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "TopicWeights");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TopicWeights");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TopicWeights");
        }
    }
}
