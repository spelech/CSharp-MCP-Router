-- ============================================================================
-- Enterprise MCP Gateway Stored Procedures (MS SQL Server)
-- ============================================================================

USE [McpEnterpriseDb];
GO

-- 1. Procedure: Evaluate User Access for a specific Tool/Item and Group SIDs/Names
CREATE OR ALTER PROCEDURE [dbo].[sp_EvaluateUserAccess]
    @GroupNames NVARCHAR(MAX),
    @ItemName VARCHAR(150),
    @RequestMethod VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    -- Compute target keys (tool:ItemName, server:ItemName, etc.)
    -- ItemName might contain "__" for tools or prompts
    DECLARE @ServerId VARCHAR(100);
    SET @ServerId = @ItemName;
    IF CHARINDEX('__', @ItemName) > 0
    BEGIN
        SET @ServerId = SUBSTRING(@ItemName, 1, CHARINDEX('__', @ItemName) - 1);
    END;

    -- Create temp table or table variable of target keys
    DECLARE @TargetKeys TABLE (TargetId VARCHAR(250));
    INSERT INTO @TargetKeys VALUES
        ('tool:' + @ItemName),
        ('prompt:' + @ItemName),
        ('resource:' + @ItemName),
        ('server:' + @ServerId);

    -- Parse CSV group names
    DECLARE @Groups TABLE (GroupName NVARCHAR(256));
    INSERT INTO @Groups
    SELECT value FROM STRING_SPLIT(@GroupNames, ',');

    -- Check if there are any policies configured for these targets
    IF NOT EXISTS (
        SELECT 1 FROM [dbo].[AccessPolicies]
        WHERE [TargetId] IN (SELECT TargetId FROM @TargetKeys)
    )
    BEGIN
        SELECT 1 AS IsAllowed;
        RETURN;
    END;

    -- Check for explicit deny
    IF EXISTS (
        SELECT 1 FROM [dbo].[AccessPolicies]
        WHERE [TargetId] IN (SELECT TargetId FROM @TargetKeys)
          AND [RequiredGroup] IN (SELECT GroupName FROM @Groups)
          AND [IsAllowed] = 0
    )
    BEGIN
        SELECT 0 AS IsAllowed;
        RETURN;
    END;

    -- Check for explicit allow
    IF EXISTS (
        SELECT 1 FROM [dbo].[AccessPolicies]
        WHERE [TargetId] IN (SELECT TargetId FROM @TargetKeys)
          AND [RequiredGroup] IN (SELECT GroupName FROM @Groups)
          AND [IsAllowed] = 1
    )
    BEGIN
        SELECT 1 AS IsAllowed;
    END
    ELSE
    BEGIN
        SELECT 0 AS IsAllowed;
    END
END;
GO

-- 2. Procedure: Get allowed tools/items for given group names
CREATE OR ALTER PROCEDURE [dbo].[sp_GetAllowedItemsForGroups]
    @GroupNames NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT DISTINCT 
        t.[ToolId], 
        t.[ServerId], 
        s.[CodeName] AS ServerCodeName, 
        t.[ToolName], 
        t.[VaultSecretPath],
        t.[SecretProvider]
    FROM [dbo].[Tools] t
    INNER JOIN [dbo].[McpServers] s ON t.[ServerId] = s.[ServerId]
    INNER JOIN [dbo].[ToolAccessPolicies] tap ON t.[ToolId] = tap.[ToolId]
    INNER JOIN [dbo].[AdGroups] g ON tap.[GroupId] = g.[GroupId]
    WHERE g.[GroupName] IN (SELECT value FROM STRING_SPLIT(@GroupNames, ','))
      AND tap.[IsAllowed] = 1
      AND t.[IsEnabled] = 1
      AND s.[IsActive] = 1;
END;
GO

-- 3. Procedure: Get Secret Path and Explicit SecretProvider for a Server
CREATE OR ALTER PROCEDURE [dbo].[sp_GetServerSecrets]
    @ServerCodeName VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        s.[ServerId],
        s.[CodeName],
        s.[SecretProvider] AS ServerSecretProvider,
        t.[ToolName],
        t.[VaultSecretPath],
        t.[SecretProvider] AS ToolSecretProvider
    FROM [dbo].[McpServers] s
    LEFT JOIN [dbo].[Tools] t ON s.[ServerId] = t.[ServerId]
    WHERE s.[CodeName] = @ServerCodeName
      AND s.[IsActive] = 1;
END;
GO

-- 4. Procedure: Save or Update Secret Provider Configuration
CREATE OR ALTER PROCEDURE [dbo].[sp_SaveSecretProvider]
    @ProviderName VARCHAR(50),
    @DisplayName NVARCHAR(100),
    @EncryptedConfigJson NVARCHAR(MAX),
    @IsEnabled BIT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF EXISTS (SELECT 1 FROM [dbo].[SecretProviders] WHERE [ProviderName] = @ProviderName)
    BEGIN
        UPDATE [dbo].[SecretProviders]
        SET [DisplayName] = @DisplayName,
            [EncryptedConfigJson] = @EncryptedConfigJson,
            [IsEnabled] = @IsEnabled,
            [UpdatedAt] = SYSUTCDATETIME()
        WHERE [ProviderName] = @ProviderName;
    END
    ELSE
    BEGIN
        INSERT INTO [dbo].[SecretProviders] ([ProviderName], [DisplayName], [EncryptedConfigJson], [IsEnabled])
        VALUES (@ProviderName, @DisplayName, @EncryptedConfigJson, @IsEnabled);
    END
END;
GO

-- 5. Procedure: Save or Update Identity/Auth Provider Configuration
CREATE OR ALTER PROCEDURE [dbo].[sp_SaveAuthProvider]
    @ProviderName VARCHAR(50),
    @DisplayName NVARCHAR(100),
    @UserHeader VARCHAR(100),
    @GroupsHeader VARCHAR(100),
    @IsEnabled BIT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF EXISTS (SELECT 1 FROM [dbo].[AuthProviderConfigs] WHERE [ProviderName] = @ProviderName)
    BEGIN
        UPDATE [dbo].[AuthProviderConfigs]
        SET [DisplayName] = @DisplayName,
            [UserHeader] = @UserHeader,
            [GroupsHeader] = @GroupsHeader,
            [IsEnabled] = @IsEnabled,
            [UpdatedAt] = SYSUTCDATETIME()
        WHERE [ProviderName] = @ProviderName;
    END
    ELSE
    BEGIN
        INSERT INTO [dbo].[AuthProviderConfigs] ([ProviderName], [DisplayName], [UserHeader], [GroupsHeader], [IsEnabled])
        VALUES (@ProviderName, @DisplayName, @UserHeader, @GroupsHeader, @IsEnabled);
    END
END;
GO

-- 6. Procedure: Insert Audit Log Entry
CREATE OR ALTER PROCEDURE [dbo].[sp_InsertAuditLog]
    @RequestId VARCHAR(64),
    @UserPrincipalName NVARCHAR(256),
    @UserSid VARCHAR(180),
    @ServerCodeName VARCHAR(100),
    @ItemName VARCHAR(150),
    @RequestMethod VARCHAR(50),
    @ExecutionTimeMs INT,
    @StatusCode INT,
    @RequestPayload NVARCHAR(MAX) = NULL,
    @ResponsePayload NVARCHAR(MAX) = NULL,
    @ErrorMessage NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO [dbo].[AuditLogs] (
        [RequestId], [UserPrincipalName], [UserSid], [ServerCodeName],
        [ItemName], [RequestMethod], [ExecutionTimeMs], [StatusCode],
        [RequestPayload], [ResponsePayload], [ErrorMessage], [Timestamp]
    )
    VALUES (
        @RequestId, @UserPrincipalName, @UserSid, @ServerCodeName,
        @ItemName, @RequestMethod, @ExecutionTimeMs, @StatusCode,
        @RequestPayload, @ResponsePayload, @ErrorMessage, SYSUTCDATETIME()
    );
END;
GO

-- 7. Procedure: Save or Update AppKey Configuration
CREATE OR ALTER PROCEDURE [dbo].[sp_SaveAppKey]
    @Id VARCHAR(100),
    @Name NVARCHAR(200),
    @Username NVARCHAR(256),
    @KeyPrefix VARCHAR(50),
    @EncryptedKey NVARCHAR(MAX),
    @ScopesJson NVARCHAR(MAX),
    @ExpiresAt DATETIME2 = NULL
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
            [ExpiresAt] = @ExpiresAt
        WHERE [Id] = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO [dbo].[AppKeys] ([Id], [Name], [Username], [KeyPrefix], [EncryptedKey], [ScopesJson], [ExpiresAt], [CreatedAt])
        VALUES (@Id, @Name, @Username, @KeyPrefix, @EncryptedKey, @ScopesJson, @ExpiresAt, SYSUTCDATETIME());
    END
END;
GO

-- 8. Procedure: Delete/Revoke AppKey
CREATE OR ALTER PROCEDURE [dbo].[sp_DeleteAppKey]
    @Id VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM [dbo].[AppKeys] WHERE [Id] = @Id;
END;
GO

-- 9. Procedure: Get AppKeys
CREATE OR ALTER PROCEDURE [dbo].[sp_GetAppKeys]
    @Username NVARCHAR(256) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @Username IS NULL
    BEGIN
        SELECT [Id], [Name], [Username], [KeyPrefix], [EncryptedKey], [ScopesJson], [ExpiresAt], [CreatedAt]
        FROM [dbo].[AppKeys];
    END
    ELSE
    BEGIN
        SELECT [Id], [Name], [Username], [KeyPrefix], [EncryptedKey], [ScopesJson], [ExpiresAt], [CreatedAt]
        FROM [dbo].[AppKeys]
        WHERE [Username] = @Username;
    END
END;
GO

-- 10. Procedure: Insert Admin Audit Log Entry
CREATE OR ALTER PROCEDURE [dbo].[sp_InsertAdminAuditLog]
    @Id VARCHAR(50),
    @Username NVARCHAR(256),
    @Action VARCHAR(100),
    @Target VARCHAR(256),
    @Details NVARCHAR(MAX) = NULL,
    @Success BIT,
    @ErrorMessage NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[AdminAuditLogs] (
        [Id], [Username], [Action], [Target], [Details], [Success], [ErrorMessage], [Timestamp]
    )
    VALUES (
        @Id, @Username, @Action, @Target, @Details, @Success, @ErrorMessage, SYSUTCDATETIME()
    );
END;
GO
