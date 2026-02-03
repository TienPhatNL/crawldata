using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTierAddPlanId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_SubscriptionTier",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Users_SubscriptionTier",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPlans_Tier",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "SubscriptionTier",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "SubscriptionTier",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SubscriptionTier",
                table: "UserQuotaSnapshots");

            migrationBuilder.DropColumn(
                name: "SubscriptionTier",
                table: "SubscriptionPayments");

            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionPlanId",
                table: "UserSubscriptions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentSubscriptionPlanId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionPlanId",
                table: "UserQuotaSnapshots",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionPlanId",
                table: "SubscriptionPayments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_SubscriptionPlanId",
                table: "UserSubscriptions",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CurrentSubscriptionPlanId",
                table: "Users",
                column: "CurrentSubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_UserQuotaSnapshots_SubscriptionPlanId",
                table: "UserQuotaSnapshots",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_IsActive",
                table: "SubscriptionPlans",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Tier",
                table: "SubscriptionPlans",
                column: "Tier");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_SubscriptionPlanId",
                table: "SubscriptionPayments",
                column: "SubscriptionPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionPayments_SubscriptionPlans_SubscriptionPlanId",
                table: "SubscriptionPayments",
                column: "SubscriptionPlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserQuotaSnapshots_SubscriptionPlans_SubscriptionPlanId",
                table: "UserQuotaSnapshots",
                column: "SubscriptionPlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_SubscriptionPlans_CurrentSubscriptionPlanId",
                table: "Users",
                column: "CurrentSubscriptionPlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSubscriptions_SubscriptionPlans_SubscriptionPlanId",
                table: "UserSubscriptions",
                column: "SubscriptionPlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionPayments_SubscriptionPlans_SubscriptionPlanId",
                table: "SubscriptionPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_UserQuotaSnapshots_SubscriptionPlans_SubscriptionPlanId",
                table: "UserQuotaSnapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_SubscriptionPlans_CurrentSubscriptionPlanId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscriptions_SubscriptionPlans_SubscriptionPlanId",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_SubscriptionPlanId",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Users_CurrentSubscriptionPlanId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserQuotaSnapshots_SubscriptionPlanId",
                table: "UserQuotaSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPlans_IsActive",
                table: "SubscriptionPlans");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPlans_Tier",
                table: "SubscriptionPlans");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPayments_SubscriptionPlanId",
                table: "SubscriptionPayments");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlanId",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "CurrentSubscriptionPlanId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlanId",
                table: "UserQuotaSnapshots");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlanId",
                table: "SubscriptionPayments");

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionTier",
                table: "UserSubscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionTier",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionTier",
                table: "UserQuotaSnapshots",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionTier",
                table: "SubscriptionPayments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_SubscriptionTier",
                table: "UserSubscriptions",
                column: "SubscriptionTier");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SubscriptionTier",
                table: "Users",
                column: "SubscriptionTier");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Tier",
                table: "SubscriptionPlans",
                column: "Tier",
                unique: true);
        }
    }
}
