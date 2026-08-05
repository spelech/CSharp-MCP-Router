-- ============================================================================
-- Enterprise MCP Gateway Database Tables (MS SQL Server)
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'McpEnterpriseDb')
BEGIN
    CREATE DATABASE [McpEnterpriseDb];
END;
GO

USE [McpEnterpriseDb];
GO

-- 1. Registered MCP Servers
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'McpServers')
BEGIN
    CREATE TABLE [dbo].[McpServers] (
        [ServerId]          INT IDENTITY(1,1) PRIMARY KEY,
        [CodeName]          VARCHAR(100) NOT NULL UNIQUE,
        [DisplayName]       NVARCHAR(200) NOT NULL,
        [Description]       NVARCHAR(MAX) NULL,
        [BaseUrl]           VARCHAR(500) NOT NULL,
        [TransportType]     VARCHAR(20) NOT NULL DEFAULT 'SSE',
        [SecretProvider]    VARCHAR(50) NOT NULL DEFAULT 'Vault',
        [HealthStatus]      VARCHAR(20) NOT NULL DEFAULT 'UNKNOWN',
        [HealthCheckUrl]    VARCHAR(500) NULL,
        [IsActive]          BIT NOT NULL DEFAULT 1,
        [CreatedAt]         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;
GO

-- 2. Secret Providers Configuration Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SecretProviders')
BEGIN
    CREATE TABLE [dbo].[SecretProviders] (
        [ProviderId]          INT IDENTITY(1,1) PRIMARY KEY,
        [ProviderName]        VARCHAR(50) NOT NULL UNIQUE, -- 'Vault', 'WindowsRegistry', 'Environment'
        [DisplayName]         NVARCHAR(100) NOT NULL,
        [EncryptedConfigJson] NVARCHAR(MAX) NULL,
        [IsEnabled]           BIT NOT NULL DEFAULT 1,
        [UpdatedAt]           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;
GO

-- 3. Identity & Auth Providers Configuration Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuthProviderConfigs')
BEGIN
    CREATE TABLE [dbo].[AuthProviderConfigs] (
        [AuthId]              INT IDENTITY(1,1) PRIMARY KEY,
        [ProviderName]        VARCHAR(50) NOT NULL UNIQUE, -- 'ActiveDirectory', 'PocketID_TinyAuth'
        [DisplayName]         NVARCHAR(100) NOT NULL,
        [UserHeader]          VARCHAR(100) NULL DEFAULT 'Remote-User',
        [GroupsHeader]        VARCHAR(100) NULL DEFAULT 'Remote-Groups',
        [IsEnabled]           BIT NOT NULL DEFAULT 1,
        [UpdatedAt]           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;
GO

-- 4. User & Group Security Groups
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AdGroups')
BEGIN
    CREATE TABLE [dbo].[AdGroups] (
        [GroupId]           INT IDENTITY(1,1) PRIMARY KEY,
        [ObjectSid]         VARCHAR(180) NOT NULL UNIQUE,
        [GroupName]         NVARCHAR(256) NOT NULL,
        [Description]       NVARCHAR(500) NULL,
        [IsActive]          BIT NOT NULL DEFAULT 1,
        [CreatedAt]         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;
GO

-- 5. Tools Registry & Access Control
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tools')
BEGIN
    CREATE TABLE [dbo].[Tools] (
        [ToolId]            INT IDENTITY(1,1) PRIMARY KEY,
        [ServerId]          INT NOT NULL FOREIGN KEY REFERENCES [dbo].[McpServers]([ServerId]),
        [ToolName]          VARCHAR(150) NOT NULL,
        [Description]       NVARCHAR(MAX) NULL,
        [InputSchemaJson]   NVARCHAR(MAX) NULL,
        [VaultSecretPath]   VARCHAR(250) NULL,
        [SecretProvider]    VARCHAR(50) NOT NULL DEFAULT 'Vault',
        [IsEnabled]         BIT NOT NULL DEFAULT 1,
        [CreatedAt]         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [UQ_Server_ToolName] UNIQUE ([ServerId], [ToolName])
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ToolAccessPolicies')
BEGIN
    CREATE TABLE [dbo].[ToolAccessPolicies] (
        [ToolPolicyId]      INT IDENTITY(1,1) PRIMARY KEY,
        [ToolId]            INT NOT NULL FOREIGN KEY REFERENCES [dbo].[Tools]([ToolId]),
        [GroupId]           INT NOT NULL FOREIGN KEY REFERENCES [dbo].[AdGroups]([GroupId]),
        [IsAllowed]         BIT NOT NULL DEFAULT 1,
        [RateLimitPerMin]   INT NOT NULL DEFAULT 60,
        [CreatedAt]         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [UQ_Tool_Group] UNIQUE ([ToolId], [GroupId])
    );
END;
GO

-- AccessPolicies Generic Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AccessPolicies')
BEGIN
    CREATE TABLE [dbo].[AccessPolicies] (
        [Id]            VARCHAR(100) PRIMARY KEY,
        [TargetId]      VARCHAR(250) NOT NULL,
        [RequiredGroup] NVARCHAR(256) NOT NULL,
        [IsAllowed]     BIT NOT NULL DEFAULT 1,
        [CreatedAt]     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;
GO

-- Group Mappings Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GroupMappings')
BEGIN
    CREATE TABLE [dbo].[GroupMappings] (
        [Id]             VARCHAR(100) PRIMARY KEY,
        [ExternalId]     VARCHAR(256) NOT NULL,
        [InternalGroup]  NVARCHAR(256) NOT NULL,
        [CreatedAt]      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;
GO

-- 6. Audit Logging Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs')
BEGIN
    CREATE TABLE [dbo].[AuditLogs] (
        [AuditId]           BIGINT IDENTITY(1,1) PRIMARY KEY,
        [RequestId]         VARCHAR(64) NOT NULL,
        [UserPrincipalName] NVARCHAR(256) NOT NULL,
        [UserSid]           VARCHAR(180) NOT NULL,
        [ServerCodeName]    VARCHAR(100) NOT NULL,
        [ItemName]          VARCHAR(150) NULL,
        [RequestMethod]     VARCHAR(50) NOT NULL,
        [ExecutionTimeMs]   INT NOT NULL,
        [StatusCode]        INT NOT NULL,
        [RequestPayload]    NVARCHAR(MAX) NULL,
        [ResponsePayload]   NVARCHAR(MAX) NULL,
        [ErrorMessage]      NVARCHAR(MAX) NULL,
        [Timestamp]         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;
GO
