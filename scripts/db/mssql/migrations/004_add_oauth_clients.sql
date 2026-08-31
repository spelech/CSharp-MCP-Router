-- Migration: Add OAuthClients table and stored procedures
-- Target: Microsoft SQL Server

USE [McpEnterpriseDb];
GO

-- 1. Create OAuthClients table
IF OBJECT_ID('dbo.OAuthClients', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OAuthClients] (
        [ClientId]         VARCHAR(100) PRIMARY KEY,
        [ClientSecretHash] VARCHAR(256) NOT NULL DEFAULT '',
        [ClientName]       NVARCHAR(200) NOT NULL,
        [ClientType]       VARCHAR(50) NOT NULL DEFAULT 'confidential',
        [RedirectUrisJson] NVARCHAR(MAX) NOT NULL DEFAULT '[]',
        [GrantTypesJson]   NVARCHAR(MAX) NOT NULL DEFAULT '[]',
        [ScopesJson]       NVARCHAR(MAX) NOT NULL DEFAULT '[]',
        [OwnerSid]         NVARCHAR(200) NOT NULL DEFAULT '',
        [CreatedBy]        NVARCHAR(256) NOT NULL DEFAULT '',
        [ExpiresAt]        DATETIME2 NULL,
        [CreatedAt]        DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;
GO

-- 2. Stored Procedure: sp_SaveOAuthClient
CREATE OR ALTER PROCEDURE [dbo].[sp_SaveOAuthClient]
    @ClientId VARCHAR(100),
    @ClientSecretHash VARCHAR(256) = '',
    @ClientName NVARCHAR(200),
    @ClientType VARCHAR(50) = 'confidential',
    @RedirectUrisJson NVARCHAR(MAX) = '[]',
    @GrantTypesJson NVARCHAR(MAX) = '[]',
    @ScopesJson NVARCHAR(MAX) = '[]',
    @OwnerSid NVARCHAR(200) = '',
    @CreatedBy NVARCHAR(256) = '',
    @ExpiresAt DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM [dbo].[OAuthClients] WHERE [ClientId] = @ClientId)
    BEGIN
        UPDATE [dbo].[OAuthClients]
        SET [ClientSecretHash] = @ClientSecretHash,
            [ClientName] = @ClientName,
            [ClientType] = @ClientType,
            [RedirectUrisJson] = @RedirectUrisJson,
            [GrantTypesJson] = @GrantTypesJson,
            [ScopesJson] = @ScopesJson,
            [OwnerSid] = @OwnerSid,
            [CreatedBy] = @CreatedBy,
            [ExpiresAt] = @ExpiresAt
        WHERE [ClientId] = @ClientId;
    END
    ELSE
    BEGIN
        INSERT INTO [dbo].[OAuthClients] (
            [ClientId], [ClientSecretHash], [ClientName], [ClientType],
            [RedirectUrisJson], [GrantTypesJson], [ScopesJson],
            [OwnerSid], [CreatedBy], [ExpiresAt], [CreatedAt]
        )
        VALUES (
            @ClientId, @ClientSecretHash, @ClientName, @ClientType,
            @RedirectUrisJson, @GrantTypesJson, @ScopesJson,
            @OwnerSid, @CreatedBy, @ExpiresAt, SYSUTCDATETIME()
        );
    END
END;
GO

-- 3. Stored Procedure: sp_GetOAuthClients
CREATE OR ALTER PROCEDURE [dbo].[sp_GetOAuthClients]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT [ClientId], [ClientSecretHash], [ClientName], [ClientType],
           [RedirectUrisJson], [GrantTypesJson], [ScopesJson],
           [OwnerSid], [CreatedBy], [ExpiresAt], [CreatedAt]
    FROM [dbo].[OAuthClients]
    ORDER BY [CreatedAt] DESC;
END;
GO

-- 4. Stored Procedure: sp_GetOAuthClientById
CREATE OR ALTER PROCEDURE [dbo].[sp_GetOAuthClientById]
    @ClientId VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT [ClientId], [ClientSecretHash], [ClientName], [ClientType],
           [RedirectUrisJson], [GrantTypesJson], [ScopesJson],
           [OwnerSid], [CreatedBy], [ExpiresAt], [CreatedAt]
    FROM [dbo].[OAuthClients]
    WHERE [ClientId] = @ClientId;
END;
GO

-- 5. Stored Procedure: sp_DeleteOAuthClient
CREATE OR ALTER PROCEDURE [dbo].[sp_DeleteOAuthClient]
    @ClientId VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM [dbo].[OAuthClients] WHERE [ClientId] = @ClientId;
END;
GO
