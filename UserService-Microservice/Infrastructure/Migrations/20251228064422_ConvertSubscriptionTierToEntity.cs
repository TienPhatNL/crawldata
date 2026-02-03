using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertSubscriptionTierToEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPlans_Tier",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "SubscriptionPlans");

            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionTierId",
                table: "SubscriptionPlans",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "SubscriptionTiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionTiers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_SubscriptionTierId",
                table: "SubscriptionPlans",
                column: "SubscriptionTierId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionTiers_IsActive",
                table: "SubscriptionTiers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionTiers_Level",
                table: "SubscriptionTiers",
                column: "Level",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionPlans_SubscriptionTiers_SubscriptionTierId",
                table: "SubscriptionPlans",
                column: "SubscriptionTierId",
                principalTable: "SubscriptionTiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionPlans_SubscriptionTiers_SubscriptionTierId",
                table: "SubscriptionPlans");

            migrationBuilder.DropTable(
                name: "SubscriptionTiers");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPlans_SubscriptionTierId",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "SubscriptionTierId",
                table: "SubscriptionPlans");

            migrationBuilder.AddColumn<int>(
                name: "Tier",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Tier",
                table: "SubscriptionPlans",
                column: "Tier");
        }
    }
}
