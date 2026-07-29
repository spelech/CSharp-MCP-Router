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
    
    -- Admin bypass check or explicit group policy match
    IF EXISTS (
        SELECT 1
        FROM [dbo].[ToolAccessPolicies] tap
        INNER JOIN [dbo].[Tools] t ON tap.[ToolId] = t.[ToolId]
        INNER JOIN [dbo].[AdGroups] g ON tap.[GroupId] = g.[GroupId]
        INNER JOIN [dbo].[McpServers] s ON t.[ServerId] = s.[ServerId]
        WHERE g.[GroupName] IN (SELECT value FROM STRING_SPLIT(@GroupNames, ','))
          AND t.[ToolName] = @ItemName
          AND tap.[IsAllowed] = 1
          AND t.[IsEnabled] = 1
          AND s.[IsActive] = 1
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
