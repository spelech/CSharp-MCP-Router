-- Migration: Add OAuthClients table and stored procedures
-- Target: MySQL / MariaDB

USE `McpEnterpriseDb`;

-- 1. Create OAuthClients table
CREATE TABLE IF NOT EXISTS `OAuthClients` (
    `ClientId`         VARCHAR(100) PRIMARY KEY,
    `ClientSecretHash` VARCHAR(256) NOT NULL DEFAULT '',
    `ClientName`       VARCHAR(200) NOT NULL,
    `ClientType`       VARCHAR(50) NOT NULL DEFAULT 'confidential',
    `RedirectUrisJson` LONGTEXT NOT NULL,
    `GrantTypesJson`   LONGTEXT NOT NULL,
    `ScopesJson`       LONGTEXT NOT NULL,
    `OwnerSid`         VARCHAR(200) NOT NULL DEFAULT '',
    `CreatedBy`        VARCHAR(256) NOT NULL DEFAULT '',
    `ExpiresAt`        DATETIME NULL,
    `CreatedAt`        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

DELIMITER //

-- 2. Stored Procedure: sp_SaveOAuthClient
DROP PROCEDURE IF EXISTS `sp_SaveOAuthClient` //
CREATE PROCEDURE `sp_SaveOAuthClient`(
    IN p_ClientId VARCHAR(100),
    IN p_ClientSecretHash VARCHAR(256),
    IN p_ClientName VARCHAR(200),
    IN p_ClientType VARCHAR(50),
    IN p_RedirectUrisJson LONGTEXT,
    IN p_GrantTypesJson LONGTEXT,
    IN p_ScopesJson LONGTEXT,
    IN p_OwnerSid VARCHAR(200),
    IN p_CreatedBy VARCHAR(256),
    IN p_ExpiresAt DATETIME
)
BEGIN
    INSERT INTO `OAuthClients` (
        `ClientId`, `ClientSecretHash`, `ClientName`, `ClientType`,
        `RedirectUrisJson`, `GrantTypesJson`, `ScopesJson`,
        `OwnerSid`, `CreatedBy`, `ExpiresAt`, `CreatedAt`
    )
    VALUES (
        p_ClientId,
        IFNULL(p_ClientSecretHash, ''),
        p_ClientName,
        IFNULL(p_ClientType, 'confidential'),
        IFNULL(p_RedirectUrisJson, '[]'),
        IFNULL(p_GrantTypesJson, '[]'),
        IFNULL(p_ScopesJson, '[]'),
        IFNULL(p_OwnerSid, ''),
        IFNULL(p_CreatedBy, ''),
        p_ExpiresAt,
        NOW()
    )
    ON DUPLICATE KEY UPDATE
        `ClientSecretHash` = IFNULL(p_ClientSecretHash, ''),
        `ClientName` = p_ClientName,
        `ClientType` = IFNULL(p_ClientType, 'confidential'),
        `RedirectUrisJson` = IFNULL(p_RedirectUrisJson, '[]'),
        `GrantTypesJson` = IFNULL(p_GrantTypesJson, '[]'),
        `ScopesJson` = IFNULL(p_ScopesJson, '[]'),
        `OwnerSid` = IFNULL(p_OwnerSid, ''),
        `CreatedBy` = IFNULL(p_CreatedBy, ''),
        `ExpiresAt` = p_ExpiresAt;
END //

-- 3. Stored Procedure: sp_GetOAuthClients
DROP PROCEDURE IF EXISTS `sp_GetOAuthClients` //
CREATE PROCEDURE `sp_GetOAuthClients`()
BEGIN
    SELECT `ClientId`, `ClientSecretHash`, `ClientName`, `ClientType`,
           `RedirectUrisJson`, `GrantTypesJson`, `ScopesJson`,
           `OwnerSid`, `CreatedBy`, `ExpiresAt`, `CreatedAt`
    FROM `OAuthClients`
    ORDER BY `CreatedAt` DESC;
END //

-- 4. Stored Procedure: sp_GetOAuthClientById
DROP PROCEDURE IF EXISTS `sp_GetOAuthClientById` //
CREATE PROCEDURE `sp_GetOAuthClientById`(
    IN p_ClientId VARCHAR(100)
)
BEGIN
    SELECT `ClientId`, `ClientSecretHash`, `ClientName`, `ClientType`,
           `RedirectUrisJson`, `GrantTypesJson`, `ScopesJson`,
           `OwnerSid`, `CreatedBy`, `ExpiresAt`, `CreatedAt`
    FROM `OAuthClients`
    WHERE `ClientId` = p_ClientId;
END //

-- 5. Stored Procedure: sp_DeleteOAuthClient
DROP PROCEDURE IF EXISTS `sp_DeleteOAuthClient` //
CREATE PROCEDURE `sp_DeleteOAuthClient`(
    IN p_ClientId VARCHAR(100)
)
BEGIN
    DELETE FROM `OAuthClients` WHERE `ClientId` = p_ClientId;
END //

DELIMITER ;
