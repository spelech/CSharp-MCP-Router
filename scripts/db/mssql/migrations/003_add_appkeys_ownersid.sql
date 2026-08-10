-- Migration: Add OwnerSid to AppKeys table and update stored procedures
-- Target: Microsoft SQL Server

USE [McpEnterpriseDb];
GO

-- 1. Add OwnerSid to AppKeys table if it doesn't exist
IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[AppKeys]')
      AND name = 'OwnerSid'
)
BEGIN
    ALTER TABLE [dbo].[AppKeys] ADD [OwnerSid] VARCHAR(180) NULL;
END;
GO

-- 2. Update Procedure: sp_SaveAppKey
CREATE OR ALTER PROCEDURE [dbo].[sp_SaveAppKey]
    @Id VARCHAR(100),
    @Name NVARCHAR(200),
    @Username NVARCHAR(256),
    @KeyPrefix VARCHAR(50),
    @EncryptedKey NVARCHAR(MAX),
    @ScopesJson NVARCHAR(MAX),
    @ExpiresAt DATETIME2 = NULL,
    @OwnerSid VARCHAR(180) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM [dbo].[AppKeys] WHERE [Id] = @Id)
    BEGIN
        UPDATE [dbo].[AppKeys]
        SET [Name] = @Name,
            [Username] = @Username,
            [KeyPrefix] = @KeyPrefix,
            [EncryptedKey] = @EncryptedKey,
            [ScopesJson] = @ScopesJson,
            [ExpiresAt] = @ExpiresAt,
            [OwnerSid] = @OwnerSid
        WHERE [Id] = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO [dbo].[AppKeys] ([Id], [Name], [Username], [KeyPrefix], [EncryptedKey], [ScopesJson], [ExpiresAt], [OwnerSid], [CreatedAt])
        VALUES (@Id, @Name, @Username, @KeyPrefix, @EncryptedKey, @ScopesJson, @ExpiresAt, @OwnerSid, SYSUTCDATETIME());
    END
END;
GO

-- 3. Update Procedure: sp_GetAppKeys
CREATE OR ALTER PROCEDURE [dbo].[sp_GetAppKeys]
    @Username NVARCHAR(256) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @Username IS NULL
    BEGIN
        SELECT [Id], [Name], [Username], [KeyPrefix], [EncryptedKey], [ScopesJson], [ExpiresAt], [OwnerSid], [CreatedAt]
        FROM [dbo].[AppKeys];
    END
    ELSE
    BEGIN
        SELECT [Id], [Name], [Username], [KeyPrefix], [EncryptedKey], [ScopesJson], [ExpiresAt], [OwnerSid], [CreatedAt]
        FROM [dbo].[AppKeys]
        WHERE [Username] = @Username;
    END
END;
GO