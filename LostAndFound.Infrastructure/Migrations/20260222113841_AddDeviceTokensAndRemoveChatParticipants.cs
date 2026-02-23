using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostAndFound.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceTokensAndRemoveChatParticipants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop ChatParticipants only if it exists (empty DB from ReportsRefactor Step 0 never had it)
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[ChatParticipants]', 'U') IS NOT NULL
                DROP TABLE [ChatParticipants];");

            // Create DeviceTokens only if missing (empty DB already has it from Step 0)
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[DeviceTokens]', 'U') IS NULL
                BEGIN
                    CREATE TABLE [DeviceTokens] (
                        [Id] int NOT NULL IDENTITY,
                        [UserId] int NOT NULL,
                        [Token] nvarchar(500) NOT NULL,
                        [Platform] nvarchar(20) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                        [UpdatedAt] datetime2 NULL,
                        CONSTRAINT [PK_DeviceTokens] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_DeviceTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE);
                    CREATE UNIQUE INDEX [IX_DeviceTokens_UserId_Token] ON [DeviceTokens] ([UserId], [Token]);
                    CREATE INDEX [IX_DeviceTokens_UserId] ON [DeviceTokens] ([UserId]);
                END
                ELSE IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DeviceTokens_UserId' AND object_id = OBJECT_ID(N'[DeviceTokens]'))
                CREATE INDEX [IX_DeviceTokens_UserId] ON [DeviceTokens] ([UserId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceTokens");

            migrationBuilder.CreateTable(
                name: "ChatParticipants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChatSessionId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatParticipants_ChatSessions_ChatSessionId",
                        column: x => x.ChatSessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatParticipants_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatParticipants_ChatSessionId",
                table: "ChatParticipants",
                column: "ChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatParticipants_UserId",
                table: "ChatParticipants",
                column: "UserId");
        }
    }
}
