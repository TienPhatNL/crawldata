using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixCrawlerChatMessageDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add default constraints if they don't exist
            // This fixes databases where the table was created without defaults
            migrationBuilder.Sql(@"
                -- Update existing NULL values to 0 before adding default constraint
                UPDATE CrawlerChatMessages SET IsSystemMessage = 0 WHERE IsSystemMessage IS NULL;
                UPDATE CrawlerChatMessages SET IsRead = 0 WHERE IsRead IS NULL;

                -- Drop existing default constraints if they exist (for idempotency)
                IF EXISTS (SELECT * FROM sys.default_constraints 
                          WHERE parent_object_id = OBJECT_ID('CrawlerChatMessages') 
                          AND parent_column_id = (SELECT column_id FROM sys.columns 
                                                 WHERE object_id = OBJECT_ID('CrawlerChatMessages') 
                                                 AND name = 'IsSystemMessage'))
                BEGIN
                    DECLARE @ConstraintName1 nvarchar(200)
                    SELECT @ConstraintName1 = name FROM sys.default_constraints 
                    WHERE parent_object_id = OBJECT_ID('CrawlerChatMessages')
                    AND parent_column_id = (SELECT column_id FROM sys.columns 
                                           WHERE object_id = OBJECT_ID('CrawlerChatMessages') 
                                           AND name = 'IsSystemMessage')
                    EXEC('ALTER TABLE CrawlerChatMessages DROP CONSTRAINT ' + @ConstraintName1)
                END

                IF EXISTS (SELECT * FROM sys.default_constraints 
                          WHERE parent_object_id = OBJECT_ID('CrawlerChatMessages') 
                          AND parent_column_id = (SELECT column_id FROM sys.columns 
                                                 WHERE object_id = OBJECT_ID('CrawlerChatMessages') 
                                                 AND name = 'IsRead'))
                BEGIN
                    DECLARE @ConstraintName2 nvarchar(200)
                    SELECT @ConstraintName2 = name FROM sys.default_constraints 
                    WHERE parent_object_id = OBJECT_ID('CrawlerChatMessages')
                    AND parent_column_id = (SELECT column_id FROM sys.columns 
                                           WHERE object_id = OBJECT_ID('CrawlerChatMessages') 
                                           AND name = 'IsRead')
                    EXEC('ALTER TABLE CrawlerChatMessages DROP CONSTRAINT ' + @ConstraintName2)
                END

                -- Add default constraints with explicit names
                ALTER TABLE CrawlerChatMessages 
                ADD CONSTRAINT DF_CrawlerChatMessages_IsSystemMessage DEFAULT 0 FOR IsSystemMessage;

                ALTER TABLE CrawlerChatMessages 
                ADD CONSTRAINT DF_CrawlerChatMessages_IsRead DEFAULT 0 FOR IsRead;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the default constraints
            migrationBuilder.Sql(@"
                ALTER TABLE CrawlerChatMessages DROP CONSTRAINT IF EXISTS DF_CrawlerChatMessages_IsSystemMessage;
                ALTER TABLE CrawlerChatMessages DROP CONSTRAINT IF EXISTS DF_CrawlerChatMessages_IsRead;
            ");
        }
    }
}
