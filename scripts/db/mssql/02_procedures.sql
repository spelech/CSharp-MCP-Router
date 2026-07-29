-- ============================================================================
-- Enterprise MCP Gateway Stored Procedures (MS SQL Server)
-- ============================================================================

USE [McpEnterpriseDb];
GO

-- Procedure: Get allowed tools/items for given group names
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
        t.[VaultSecretPath]
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

-- Procedure: Insert Audit Log Entry
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
