using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCrawlerChatMessagesAndReportCrawlData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if CrawlerChatMessages table already exists before creating
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CrawlerChatMessages')
                BEGIN
                    CREATE TABLE [CrawlerChatMessages] (
                        [Id] uniqueidentifier NOT NULL,
                        [ConversationId] uniqueidentifier NOT NULL,
                        [ParentMessageId] uniqueidentifier NULL,
                        [AssignmentId] uniqueidentifier NOT NULL,
                        [GroupId] uniqueidentifier NULL,
                        [SenderId] uniqueidentifier NOT NULL,
                        [MessageContent] nvarchar(max) NOT NULL,
                        [MessageType] int NOT NULL,
                        [CrawlJobId] uniqueidentifier NULL,
                        [CrawlResultSummary] nvarchar(max) NULL,
                        [IsSystemMessage] bit NOT NULL DEFAULT 0,
                        [MetadataJson] nvarchar(max) NULL,
                        [IsRead] bit NOT NULL DEFAULT 0,
                        [EditedAt] datetime2 NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NULL,
                        [CreatedBy] uniqueidentifier NULL,
                        [LastModifiedBy] uniqueidentifier NULL,
                        [LastModifiedAt] datetime2 NULL,
                        CONSTRAINT [PK_CrawlerChatMessages] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_CrawlerChatMessages_Assignments_AssignmentId] FOREIGN KEY ([AssignmentId]) REFERENCES [Assignments] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_CrawlerChatMessages_CrawlerChatMessages_ParentMessageId] FOREIGN KEY ([ParentMessageId]) REFERENCES [CrawlerChatMessages] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_CrawlerChatMessages_Groups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [Groups] ([Id]) ON DELETE NO ACTION
                    );
                    
                    CREATE INDEX [IX_CrawlerChatMessages_AssignmentId] ON [CrawlerChatMessages] ([AssignmentId]);
                    CREATE INDEX [IX_CrawlerChatMessages_AssignmentId_CreatedAt] ON [CrawlerChatMessages] ([AssignmentId], [CreatedAt]);
                    CREATE INDEX [IX_CrawlerChatMessages_ConversationId] ON [CrawlerChatMessages] ([ConversationId]);
                    CREATE INDEX [IX_CrawlerChatMessages_ConversationId_CreatedAt] ON [CrawlerChatMessages] ([ConversationId], [CreatedAt]);
                    CREATE INDEX [IX_CrawlerChatMessages_CrawlJobId] ON [CrawlerChatMessages] ([CrawlJobId]);
                    CREATE INDEX [IX_CrawlerChatMessages_GroupId] ON [CrawlerChatMessages] ([GroupId]);
                    CREATE INDEX [IX_CrawlerChatMessages_ParentMessageId] ON [CrawlerChatMessages] ([ParentMessageId]);
                    CREATE INDEX [IX_CrawlerChatMessages_SenderId] ON [CrawlerChatMessages] ([SenderId]);
                END
            ");

            // Check if ReportCrawlData table already exists before creating
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReportCrawlData')
                BEGIN
                    CREATE TABLE [ReportCrawlData] (
                        [Id] uniqueidentifier NOT NULL,
                        [ReportId] uniqueidentifier NOT NULL,
                        [CrawlJobId] uniqueidentifier NOT NULL,
                        [ConversationId] uniqueidentifier NOT NULL,
                        [DataSummary] nvarchar(max) NOT NULL,
                        [SourceUrl] nvarchar(2000) NOT NULL,
                        [Title] nvarchar(500) NOT NULL,
                        [LinkedAt] datetime2 NOT NULL,
                        [LinkedBy] uniqueidentifier NOT NULL,
                        [DisplayOrder] int NOT NULL DEFAULT 0,
                        [IsIncludedInSubmission] bit NOT NULL DEFAULT 1,
                        [Notes] nvarchar(1000) NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NULL,
                        CONSTRAINT [PK_ReportCrawlData] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_ReportCrawlData_Reports_ReportId] FOREIGN KEY ([ReportId]) REFERENCES [Reports] ([Id]) ON DELETE CASCADE
                    );
                    
                    CREATE INDEX [IX_ReportCrawlData_ConversationId] ON [ReportCrawlData] ([ConversationId]);
                    CREATE INDEX [IX_ReportCrawlData_CrawlJobId] ON [ReportCrawlData] ([CrawlJobId]);
                    CREATE INDEX [IX_ReportCrawlData_ReportId] ON [ReportCrawlData] ([ReportId]);
                    CREATE INDEX [IX_ReportCrawlData_ReportId_DisplayOrder] ON [ReportCrawlData] ([ReportId], [DisplayOrder]);
                    CREATE INDEX [IX_ReportCrawlData_ReportId_IsIncludedInSubmission] ON [ReportCrawlData] ([ReportId], [IsIncludedInSubmission]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS [CrawlerChatMessages]");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [ReportCrawlData]");
        }
    }
}
