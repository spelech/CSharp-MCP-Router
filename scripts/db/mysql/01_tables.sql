-- ============================================================================
-- Enterprise MCP Gateway Database Tables (MySQL / MariaDB)
-- ============================================================================

CREATE DATABASE IF NOT EXISTS `McpEnterpriseDb`;
USE `McpEnterpriseDb`;

-- 1. Registered MCP Servers
CREATE TABLE IF NOT EXISTS `McpServers` (
    `ServerId`          INT AUTO_INCREMENT PRIMARY KEY,
    `CodeName`          VARCHAR(100) NOT NULL UNIQUE,
    `DisplayName`       VARCHAR(200) NOT NULL,
    `Description`       TEXT NULL,
    `BaseUrl`           VARCHAR(500) NOT NULL,
    `TransportType`     VARCHAR(20) NOT NULL DEFAULT 'SSE',
    `SecretProvider`    VARCHAR(50) NOT NULL DEFAULT 'Vault',
    `HealthStatus`      VARCHAR(20) NOT NULL DEFAULT 'UNKNOWN',
    `HealthCheckUrl`    VARCHAR(500) NULL,
    `IsActive`          TINYINT(1) NOT NULL DEFAULT 1,
    `CreatedAt`         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt`         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 2. Secret Providers Configuration Table
CREATE TABLE IF NOT EXISTS `SecretProviders` (
    `ProviderId`          INT AUTO_INCREMENT PRIMARY KEY,
    `ProviderName`        VARCHAR(50) NOT NULL UNIQUE,
    `DisplayName`         VARCHAR(100) NOT NULL,
    `EncryptedConfigJson` LONGTEXT NULL,
    `IsEnabled`           TINYINT(1) NOT NULL DEFAULT 1,
    `UpdatedAt`           DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 3. Identity & Auth Providers Configuration Table
CREATE TABLE IF NOT EXISTS `AuthProviderConfigs` (
    `AuthId`              INT AUTO_INCREMENT PRIMARY KEY,
    `ProviderName`        VARCHAR(50) NOT NULL UNIQUE,
    `DisplayName`         VARCHAR(100) NOT NULL,
    `UserHeader`          VARCHAR(100) NULL DEFAULT 'Remote-User',
    `GroupsHeader`        VARCHAR(100) NULL DEFAULT 'Remote-Groups',
    `IsEnabled`           TINYINT(1) NOT NULL DEFAULT 1,
    `UpdatedAt`           DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 4. User & Group Security Groups
CREATE TABLE IF NOT EXISTS `AdGroups` (
    `GroupId`           INT AUTO_INCREMENT PRIMARY KEY,
    `ObjectSid`         VARCHAR(180) NOT NULL UNIQUE,
    `GroupName`         VARCHAR(256) NOT NULL,
    `Description`       VARCHAR(500) NULL,
    `IsActive`          TINYINT(1) NOT NULL DEFAULT 1,
    `CreatedAt`         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 5. Tools Registry & Access Control
CREATE TABLE IF NOT EXISTS `Tools` (
    `ToolId`            INT AUTO_INCREMENT PRIMARY KEY,
    `ServerId`          INT NOT NULL,
    `ToolName`          VARCHAR(150) NOT NULL,
    `Description`       TEXT NULL,
    `InputSchemaJson`   LONGTEXT NULL,
    `VaultSecretPath`   VARCHAR(250) NULL,
    `SecretProvider`    VARCHAR(50) NOT NULL DEFAULT 'Vault',
    `IsEnabled`         TINYINT(1) NOT NULL DEFAULT 1,
    `CreatedAt`         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT `FK_Tools_McpServers` FOREIGN KEY (`ServerId`) REFERENCES `McpServers` (`ServerId`) ON DELETE CASCADE,
    CONSTRAINT `UQ_Server_ToolName` UNIQUE (`ServerId`, `ToolName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ToolAccessPolicies` (
    `ToolPolicyId`      INT AUTO_INCREMENT PRIMARY KEY,
    `ToolId`            INT NOT NULL,
    `GroupId`           INT NOT NULL,
    `IsAllowed`         TINYINT(1) NOT NULL DEFAULT 1,
    `RateLimitPerMin`   INT NOT NULL DEFAULT 60,
    `CreatedAt`         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT `FK_ToolAccessPolicies_Tools` FOREIGN KEY (`ToolId`) REFERENCES `Tools` (`ToolId`) ON DELETE CASCADE,
    CONSTRAINT `FK_ToolAccessPolicies_AdGroups` FOREIGN KEY (`GroupId`) REFERENCES `AdGroups` (`GroupId`) ON DELETE CASCADE,
    CONSTRAINT `UQ_Tool_Group` UNIQUE (`ToolId`, `GroupId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 6. Audit Logging Table
CREATE TABLE IF NOT EXISTS `AuditLogs` (
    `AuditId`           BIGINT AUTO_INCREMENT PRIMARY KEY,
    `RequestId`         VARCHAR(64) NOT NULL,
    `UserPrincipalName` VARCHAR(256) NOT NULL,
    `UserSid`           VARCHAR(180) NOT NULL,
    `ServerCodeName`    VARCHAR(100) NOT NULL,
    `ItemName`          VARCHAR(150) NULL,
    `RequestMethod`     VARCHAR(50) NOT NULL,
    `ExecutionTimeMs`   INT NOT NULL,
    `StatusCode`        INT NOT NULL,
    `RequestPayload`    LONGTEXT NULL,
    `ResponsePayload`   LONGTEXT NULL,
    `ErrorMessage`      LONGTEXT NULL,
    `Timestamp`         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
