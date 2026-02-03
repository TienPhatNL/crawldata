using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorGroupMemberToUseEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add new EnrollmentId column as nullable first
            migrationBuilder.AddColumn<Guid>(
                name: "EnrollmentId_New",
                table: "GroupMembers",
                type: "uniqueidentifier",
                nullable: true);

            // Step 2: Backfill EnrollmentId from existing StudentId
            // Map each GroupMember.StudentId to the corresponding active CourseEnrollment
            migrationBuilder.Sql(@"
                UPDATE gm
                SET gm.EnrollmentId_New = ce.Id
                FROM GroupMembers gm
                INNER JOIN Groups g ON gm.GroupId = g.Id
                INNER JOIN CourseEnrollments ce ON ce.CourseId = g.CourseId 
                    AND ce.StudentId = gm.StudentId 
                    AND ce.Status = 1;  -- Active status
            ");

            // Step 3: Log/Handle orphaned records (GroupMembers without matching active enrollment)
            // Option: Delete them or keep them for manual review
            migrationBuilder.Sql(@"
                -- Check for orphaned records (should log these in production)
                IF EXISTS (SELECT 1 FROM GroupMembers WHERE EnrollmentId_New IS NULL)
                BEGIN
                    -- Option 1: Delete orphaned records
                    DELETE FROM GroupMembers WHERE EnrollmentId_New IS NULL;
                    
                    -- Option 2: Keep for manual review (comment above DELETE and uncomment below)
                    -- RAISERROR('Warning: Found GroupMembers without matching active enrollment. Manual review required.', 10, 1);
                END
            ");

            // Step 4: Drop old StudentId column
            migrationBuilder.DropIndex(
                name: "IX_GroupMembers_StudentId",
                table: "GroupMembers");

            migrationBuilder.DropIndex(
                name: "IX_GroupMembers_GroupId_StudentId",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "GroupMembers");

            // Step 5: Rename new column to EnrollmentId
            migrationBuilder.RenameColumn(
                name: "EnrollmentId_New",
                table: "GroupMembers",
                newName: "EnrollmentId");

            // Step 6: Make EnrollmentId non-nullable
            migrationBuilder.AlterColumn<Guid>(
                name: "EnrollmentId",
                table: "GroupMembers",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            // Step 7: Create new indexes
            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_EnrollmentId",
                table: "GroupMembers",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_GroupId_EnrollmentId",
                table: "GroupMembers",
                columns: new[] { "GroupId", "EnrollmentId" },
                unique: true);

            // Step 8: Add foreign key constraint with NO ACTION to avoid cascade path conflicts
            // Note: GroupMembers are already cascade-deleted through Group relationship
            migrationBuilder.AddForeignKey(
                name: "FK_GroupMembers_CourseEnrollments_EnrollmentId",
                table: "GroupMembers",
                column: "EnrollmentId",
                principalTable: "CourseEnrollments",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);  // Changed from Cascade to NoAction
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop FK constraint
            migrationBuilder.DropForeignKey(
                name: "FK_GroupMembers_CourseEnrollments_EnrollmentId",
                table: "GroupMembers");

            // Step 2: Drop new indexes
            migrationBuilder.DropIndex(
                name: "IX_GroupMembers_GroupId_EnrollmentId",
                table: "GroupMembers");

            migrationBuilder.DropIndex(
                name: "IX_GroupMembers_EnrollmentId",
                table: "GroupMembers");

            // Step 3: Add StudentId column back as nullable
            migrationBuilder.AddColumn<Guid>(
                name: "StudentId",
                table: "GroupMembers",
                type: "uniqueidentifier",
                nullable: true);

            // Step 4: Backfill StudentId from EnrollmentId
            migrationBuilder.Sql(@"
                UPDATE gm
                SET gm.StudentId = ce.StudentId
                FROM GroupMembers gm
                INNER JOIN CourseEnrollments ce ON gm.EnrollmentId = ce.Id;
            ");

            // Step 5: Make StudentId non-nullable
            migrationBuilder.AlterColumn<Guid>(
                name: "StudentId",
                table: "GroupMembers",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            // Step 6: Drop EnrollmentId column
            migrationBuilder.DropColumn(
                name: "EnrollmentId",
                table: "GroupMembers");

            // Step 7: Recreate old indexes
            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_StudentId",
                table: "GroupMembers",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_GroupId_StudentId",
                table: "GroupMembers",
                columns: new[] { "GroupId", "StudentId" },
                unique: true);
        }
    }
}
