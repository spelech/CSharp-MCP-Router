-- Migration: Align Runtime Persistence (Servers, Tools.ServerId VARCHAR(100), EncryptedConfigJson, ConfigJson, OwnerSid)
-- Target: Microsoft SQL Server

USE [McpEnterpriseDb];
GO

-- 1. Migrate McpServers to Servers table
IF OBJECT_ID('dbo.McpServers', 'U') IS NOT NULL
BEGIN
    IF OBJECT_ID('dbo.Servers', 'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[Servers] (
            [Id]                VARCHAR(100) PRIMARY KEY,
            [DisplayName]       NVARCHAR(200) NOT NULL,
            [Url]               VARCHAR(500) NOT NULL,
            [Enabled]           BIT NOT NULL DEFAULT 1,
            [Hidden]            BIT NOT NULL DEFAULT 0,
            [Type]              VARCHAR(20) NOT NULL DEFAULT 'sse',
            [SecretProvider]    VARCHAR(50) NOT NULL DEFAULT 'None',
            [SecretItemKey]     VARCHAR(100) NULL,
            [SecretMount]       VARCHAR(100) NULL,
            [SecretPath]        VARCHAR(250) NULL,
            [SecretField]       VARCHAR(100) NULL,
            [AuthShape]         VARCHAR(20) NOT NULL DEFAULT 'bearer',
            [CustomHeaderName]  VARCHAR(100) NULL,
            [Categories]        NVARCHAR(MAX) NOT NULL DEFAULT '[]',
            [ApiKey]            NVARCHAR(MAX) NULL,
            [HeadersJson]       NVARCHAR(MAX) NULL,
            [AutoDiscovered]    BIT NOT NULL DEFAULT 0
        );
    END;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.McpServers') AND name = 'Id')
    BEGIN
        INSERT INTO [dbo].[Servers] ([Id], [DisplayName], [Url], [Enabled], [Hidden], [Type], [SecretProvider], [SecretItemKey], [SecretMount], [SecretPath], [SecretField], [AuthShape], [CustomHeaderName], [Categories], [ApiKey], [HeadersJson], [AutoDiscovered])
        SELECT [Id], [DisplayName], [Url], [Enabled], [Hidden], [Type], [SecretProvider], [SecretItemKey], [SecretMount], [SecretPath], [SecretField], [AuthShape], [CustomHeaderName], [Categories], [ApiKey], [HeadersJson], [AutoDiscovered]
        FROM [dbo].[McpServers]
        WHERE [Id] NOT IN (SELECT [Id] FROM [dbo].[Servers]);
    END
    ELSE IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.McpServers') AND name = 'CodeName')
    BEGIN
        INSERT INTO [dbo].[Servers] ([Id], [DisplayName], [Url], [Enabled], [Hidden], [Type], [SecretProvider], [Categories])
        SELECT [CodeName], [DisplayName], [Url], ISNULL([IsActive], 1), 0, 'sse', ISNULL([SecretProvider], 'None'), '[]'
        FROM [dbo].[McpServers]
        WHERE [CodeName] NOT IN (SELECT [Id] FROM [dbo].[Servers]);
    END;
END;
GO

-- 2. Migrate Tools.ServerId to VARCHAR(100) if INT
IF OBJECT_ID('dbo.Tools', 'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.columns c
        JOIN sys.types t ON c.user_type_id = t.user_type_id
        WHERE c.object_id = OBJECT_ID('dbo.Tools')
          AND c.name = 'ServerId'
          AND t.name IN ('int', 'bigint', 'smallint', 'tinyint')
    )
    BEGIN
        DECLARE @sql NVARCHAR(MAX) = N'';
        SELECT @sql += N'ALTER TABLE [dbo].[Tools] DROP CONSTRAINT [' + fk.name + N'];'
        FROM sys.foreign_keys fk
        WHERE fk.parent_object_id = OBJECT_ID('dbo.Tools');
        IF @sql <> N'' EXEC sp_executesql @sql;

        SET @sql = N'';
        SELECT @sql += N'ALTER TABLE [dbo].[Tools] DROP CONSTRAINT [' + kc.name + N'];'
        FROM sys.key_constraints kc
        WHERE kc.parent_object_id = OBJECT_ID('dbo.Tools') AND kc.type = 'UQ';
        IF @sql <> N'' EXEC sp_executesql @sql;

        IF OBJECT_ID('dbo.McpServers', 'U') IS NOT NULL AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.McpServers') AND name = 'ServerId')
        BEGIN
            ALTER TABLE [dbo].[Tools] ADD [TempServerId] VARCHAR(100) NULL;
            EXEC(N'UPDATE t SET t.TempServerId = ms.CodeName FROM [dbo].[Tools] t INNER JOIN [dbo].[McpServers] ms ON t.ServerId = ms.ServerId');
            ALTER TABLE [dbo].[Tools] DROP COLUMN [ServerId];
            EXEC sp_rename 'dbo.Tools.TempServerId', 'ServerId', 'COLUMN';
        END
        ELSE
        BEGIN
            ALTER TABLE [dbo].[Tools] ALTER COLUMN [ServerId] VARCHAR(100) NOT NULL;
        END;

        ALTER TABLE [dbo].[Tools] ALTER COLUMN [ServerId] VARCHAR(100) NOT NULL;

        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Tools_Servers')
        BEGIN
            ALTER TABLE [dbo].[Tools] ADD CONSTRAINT [FK_Tools_Servers] FOREIGN KEY ([ServerId]) REFERENCES [dbo].[Servers]([Id]) ON DELETE CASCADE;
        END;
        IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_Server_ToolName')
        BEGIN
            ALTER TABLE [dbo].[Tools] ADD CONSTRAINT [UQ_Server_ToolName] UNIQUE ([ServerId], [ToolName]);
        END;
    END;
END;
GO

-- 3. Migrate SecretProviders.ConfigJson to EncryptedConfigJson
IF OBJECT_ID('dbo.SecretProviders', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SecretProviders') AND name = 'EncryptedConfigJson')
    BEGIN
        IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SecretProviders') AND name = 'ConfigJson')
        BEGIN
            ALTER TABLE [dbo].[SecretProviders] ADD [EncryptedConfigJson] NVARCHAR(MAX) NULL;
            EXEC(N'UPDATE [dbo].[SecretProviders] SET [EncryptedConfigJson] = [ConfigJson] WHERE [EncryptedConfigJson] IS NULL');
        END
        ELSE
        BEGIN
            ALTER TABLE [dbo].[SecretProviders] ADD [EncryptedConfigJson] NVARCHAR(MAX) NULL;
        END;
    END;
END;
GO

-- 4. Migrate AuthProviderConfigs.ConfigJson to EncryptedConfigJson
IF OBJECT_ID('dbo.AuthProviderConfigs', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AuthProviderConfigs') AND name = 'EncryptedConfigJson')
    BEGIN
        IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AuthProviderConfigs') AND name = 'ConfigJson')
        BEGIN
            ALTER TABLE [dbo].[AuthProviderConfigs] ADD [EncryptedConfigJson] NVARCHAR(MAX) NULL;
            EXEC(N'UPDATE [dbo].[AuthProviderConfigs] SET [EncryptedConfigJson] = [ConfigJson] WHERE [EncryptedConfigJson] IS NULL');
        END
        ELSE
        BEGIN
            ALTER TABLE [dbo].[AuthProviderConfigs] ADD [EncryptedConfigJson] NVARCHAR(MAX) NULL;
        END;
    END;
END;
GO

-- 5. Migrate AppKeys.OwnerSid
IF OBJECT_ID('dbo.AppKeys', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AppKeys') AND name = 'OwnerSid')
    BEGIN
        ALTER TABLE [dbo].[AppKeys] ADD [OwnerSid] NVARCHAR(200) NOT NULL DEFAULT '';
    END;
END;
GO
