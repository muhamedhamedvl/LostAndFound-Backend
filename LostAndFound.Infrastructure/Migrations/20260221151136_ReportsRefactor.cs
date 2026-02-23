using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostAndFound.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReportsRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Step 0: On empty DB, create base tables (Categories, SubCategories, Users, Roles, UserRoles, ChatSessions, ChatMessages, DeviceTokens) ──
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[Categories]', 'U') IS NULL
                CREATE TABLE [Categories] (
                    [Id] int NOT NULL IDENTITY,
                    [Name] nvarchar(50) NOT NULL,
                    [Description] nvarchar(200) NULL,
                    [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]));

                IF OBJECT_ID(N'[Roles]', 'U') IS NULL
                CREATE TABLE [Roles] (
                    [Id] int NOT NULL IDENTITY,
                    [Name] nvarchar(50) NOT NULL,
                    [Description] nvarchar(200) NOT NULL,
                    [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id]));
                IF OBJECT_ID(N'[Roles]', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Roles_Name' AND object_id = OBJECT_ID(N'[Roles]'))
                CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]);

                IF OBJECT_ID(N'[Users]', 'U') IS NULL
                CREATE TABLE [Users] (
                    [Id] int NOT NULL IDENTITY,
                    [FullName] nvarchar(100) NOT NULL,
                    [Email] nvarchar(100) NOT NULL,
                    [Phone] nvarchar(20) NOT NULL,
                    [PasswordHash] nvarchar(max) NOT NULL,
                    [IsVerified] bit NOT NULL DEFAULT 0,
                    [VerificationCode] nvarchar(10) NULL,
                    [VerificationCodeExpiry] datetime2 NULL,
                    [DateOfBirth] datetime2 NULL,
                    [Gender] nvarchar(20) NULL,
                    [ProfilePictureUrl] nvarchar(500) NULL,
                    [RefreshToken] nvarchar(max) NULL,
                    [RefreshTokenExpiry] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                    [UpdatedAt] datetime2 NULL,
                    [PasswordResetToken] nvarchar(max) NULL,
                    [PasswordResetTokenExpiry] datetime2 NULL,
                    [EmailChangeToken] nvarchar(max) NULL,
                    [EmailChangeTokenExpiry] datetime2 NULL,
                    [PendingEmail] nvarchar(max) NULL,
                    [DeletedAt] datetime2 NULL,
                    [IsDeleted] bit NOT NULL DEFAULT 0,
                    [GoogleId] nvarchar(100) NULL,
                    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
                    CONSTRAINT [CHK_Users_Gender] CHECK ([Gender] IN ('Male', 'Female', 'Anonymous') OR [Gender] IS NULL));
                IF OBJECT_ID(N'[Users]', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_GoogleId' AND object_id = OBJECT_ID(N'[Users]'))
                CREATE UNIQUE INDEX [IX_Users_GoogleId] ON [Users] ([GoogleId]) WHERE [GoogleId] IS NOT NULL;

                IF OBJECT_ID(N'[UserRoles]', 'U') IS NULL
                CREATE TABLE [UserRoles] (
                    [Id] int NOT NULL IDENTITY,
                    [UserId] int NOT NULL,
                    [RoleId] int NOT NULL,
                    [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE);
                IF OBJECT_ID(N'[UserRoles]', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserRoles_UserId_RoleId' AND object_id = OBJECT_ID(N'[UserRoles]'))
                CREATE UNIQUE INDEX [IX_UserRoles_UserId_RoleId] ON [UserRoles] ([UserId], [RoleId]);
                IF OBJECT_ID(N'[UserRoles]', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserRoles_RoleId' AND object_id = OBJECT_ID(N'[UserRoles]'))
                CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
                IF OBJECT_ID(N'[UserRoles]', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserRoles_UserId' AND object_id = OBJECT_ID(N'[UserRoles]'))
                CREATE INDEX [IX_UserRoles_UserId] ON [UserRoles] ([UserId]);

                IF OBJECT_ID(N'[SubCategories]', 'U') IS NULL
                CREATE TABLE [SubCategories] (
                    [Id] int NOT NULL IDENTITY,
                    [Name] nvarchar(50) NOT NULL,
                    [Description] nvarchar(200) NULL,
                    [CategoryId] int NOT NULL,
                    [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [PK_SubCategories] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_SubCategories_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION);
                IF OBJECT_ID(N'[SubCategories]', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SubCategories_CategoryId' AND object_id = OBJECT_ID(N'[SubCategories]'))
                CREATE INDEX [IX_SubCategories_CategoryId] ON [SubCategories] ([CategoryId]);

                IF OBJECT_ID(N'[ChatSessions]', 'U') IS NULL
                CREATE TABLE [ChatSessions] (
                    [Id] int NOT NULL IDENTITY,
                    [User1Id] int NOT NULL,
                    [User2Id] int NOT NULL,
                    [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                    [LastMessageTime] datetime2 NULL DEFAULT GETUTCDATE(),
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [PK_ChatSessions] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_ChatSessions_Users_User1Id] FOREIGN KEY ([User1Id]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_ChatSessions_Users_User2Id] FOREIGN KEY ([User2Id]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION);
                IF OBJECT_ID(N'[ChatSessions]', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChatSessions_User1Id' AND object_id = OBJECT_ID(N'[ChatSessions]'))
                CREATE INDEX [IX_ChatSessions_User1Id] ON [ChatSessions] ([User1Id]);
                IF OBJECT_ID(N'[ChatSessions]', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChatSessions_User2Id' AND object_id = OBJECT_ID(N'[ChatSessions]'))
                CREATE INDEX [IX_ChatSessions_User2Id] ON [ChatSessions] ([User2Id]);

                IF OBJECT_ID(N'[ChatMessages]', 'U') IS NULL
                CREATE TABLE [ChatMessages] (
                    [Id] int NOT NULL IDENTITY,
                    [ChatSessionId] int NOT NULL,
                    [SenderId] int NOT NULL,
                    [ReceiverId] int NOT NULL,
                    [Text] nvarchar(1000) NOT NULL,
                    [SentAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                    [IsRead] bit NOT NULL DEFAULT 0,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_ChatMessages_ChatSessions_ChatSessionId] FOREIGN KEY ([ChatSessionId]) REFERENCES [ChatSessions] ([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_ChatMessages_Users_ReceiverId] FOREIGN KEY ([ReceiverId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_ChatMessages_Users_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION);
                IF OBJECT_ID(N'[ChatMessages]', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChatMessages_ChatSessionId' AND object_id = OBJECT_ID(N'[ChatMessages]'))
                CREATE INDEX [IX_ChatMessages_ChatSessionId] ON [ChatMessages] ([ChatSessionId]);
                IF OBJECT_ID(N'[ChatMessages]', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChatMessages_ReceiverId' AND object_id = OBJECT_ID(N'[ChatMessages]'))
                CREATE INDEX [IX_ChatMessages_ReceiverId] ON [ChatMessages] ([ReceiverId]);
                IF OBJECT_ID(N'[ChatMessages]', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChatMessages_SenderId' AND object_id = OBJECT_ID(N'[ChatMessages]'))
                CREATE INDEX [IX_ChatMessages_SenderId] ON [ChatMessages] ([SenderId]);

                IF OBJECT_ID(N'[DeviceTokens]', 'U') IS NULL
                CREATE TABLE [DeviceTokens] (
                    [Id] int NOT NULL IDENTITY,
                    [UserId] int NOT NULL,
                    [Token] nvarchar(500) NOT NULL,
                    [Platform] nvarchar(20) NOT NULL,
                    [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [PK_DeviceTokens] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_DeviceTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE);
                IF OBJECT_ID(N'[DeviceTokens]', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DeviceTokens_UserId_Token' AND object_id = OBJECT_ID(N'[DeviceTokens]'))
                CREATE UNIQUE INDEX [IX_DeviceTokens_UserId_Token] ON [DeviceTokens] ([UserId], [Token]);
                IF OBJECT_ID(N'[DeviceTokens]', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DeviceTokens_UserId' AND object_id = OBJECT_ID(N'[DeviceTokens]'))
                CREATE INDEX [IX_DeviceTokens_UserId] ON [DeviceTokens] ([UserId]);
            ");

            // ── Step 1: Drop old tables that no longer exist in the model ──
            // Drop all FK constraints referencing Posts, then drop old tables.
            migrationBuilder.Sql(@"
                -- Drop all FK constraints that REFERENCE the Posts table (from other tables)
                DECLARE @sql NVARCHAR(MAX) = '';
                SELECT @sql += 'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(f.parent_object_id)) + '.' + QUOTENAME(OBJECT_NAME(f.parent_object_id)) + ' DROP CONSTRAINT ' + QUOTENAME(f.name) + ';' + CHAR(13)
                FROM sys.foreign_keys f
                WHERE f.referenced_object_id = OBJECT_ID(N'[Posts]');
                IF @sql <> '' EXEC sp_executesql @sql;

                -- Drop all FK constraints ON the Posts table (outgoing)
                SET @sql = '';
                SELECT @sql += 'ALTER TABLE [Posts] DROP CONSTRAINT ' + QUOTENAME(f.name) + ';' + CHAR(13)
                FROM sys.foreign_keys f
                WHERE f.parent_object_id = OBJECT_ID(N'[Posts]');
                IF @sql <> '' EXEC sp_executesql @sql;

                -- Now drop the old tables
                IF OBJECT_ID(N'[Photos]', 'U') IS NOT NULL DROP TABLE [Photos];
                IF OBJECT_ID(N'[PostImages]', 'U') IS NOT NULL DROP TABLE [PostImages];
                IF OBJECT_ID(N'[Posts]', 'U') IS NOT NULL DROP TABLE [Posts];
            ");

            // ── Step 2: Clear old migration history entries ──
            migrationBuilder.Sql(@"
                DELETE FROM [__EFMigrationsHistory]
                WHERE [MigrationId] IN ('20251129230355_init', '20251207234041_AddAuthenticationTokens')");

            // ── Step 3: Create new Reports table ──
            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    MatchPercentage = table.Column<double>(type: "float", nullable: true),
                    SubCategoryId = table.Column<int>(type: "int", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reports_SubCategories_SubCategoryId",
                        column: x => x.SubCategoryId,
                        principalTable: "SubCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reports_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ── Step 4: Create ReportImages table ──
            migrationBuilder.CreateTable(
                name: "ReportImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ReportId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportImages_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ── Step 5: Create ReportMatches table ──
            migrationBuilder.CreateTable(
                name: "ReportMatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<int>(type: "int", nullable: false),
                    MatchedReportId = table.Column<int>(type: "int", nullable: false),
                    SimilarityScore = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportMatches_Reports_MatchedReportId",
                        column: x => x.MatchedReportId,
                        principalTable: "Reports",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReportMatches_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ── Step 6: Create Notifications table (with ReportId and ActorId) ──
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[Notifications]', 'U') IS NOT NULL
                BEGIN
                    DROP TABLE [Notifications];
                END");

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportId = table.Column<int>(type: "int", nullable: true),
                    ActorId = table.Column<int>(type: "int", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            // ── Step 7: Create indexes ──
            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ActorId",
                table: "Notifications",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ReportId",
                table: "Notifications",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportImages_ReportId",
                table: "ReportImages",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportMatches_MatchedReportId",
                table: "ReportMatches",
                column: "MatchedReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportMatches_ReportId_MatchedReportId",
                table: "ReportMatches",
                columns: new[] { "ReportId", "MatchedReportId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_CreatedById",
                table: "Reports",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_SubCategoryId",
                table: "Reports",
                column: "SubCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Notifications");
            migrationBuilder.DropTable(name: "ReportImages");
            migrationBuilder.DropTable(name: "ReportMatches");
            migrationBuilder.DropTable(name: "Reports");
        }
    }
}
