-- ============================================================================
-- Enterprise MCP Gateway Stored Procedures (MySQL / MariaDB)
-- ============================================================================

USE `McpEnterpriseDb`;

DELIMITER //

-- 1. Procedure: Evaluate User Access
DROP PROCEDURE IF EXISTS `sp_EvaluateUserAccess` //
CREATE PROCEDURE `sp_EvaluateUserAccess`(
    IN p_GroupNames TEXT,
    IN p_ItemName VARCHAR(150),
    IN p_RequestMethod VARCHAR(50)
)
BEGIN
    SELECT CASE WHEN COUNT(*) > 0 THEN 1 ELSE 0 END AS IsAllowed
    FROM `ToolAccessPolicies` tap
    INNER JOIN `Tools` t ON tap.`ToolId` = t.`ToolId`
    INNER JOIN `AdGroups` g ON tap.`GroupId` = g.`GroupId`
    INNER JOIN `McpServers` s ON t.`ServerId` = s.`ServerId`
    WHERE FIND_IN_SET(g.`GroupName`, p_GroupNames) > 0
      AND t.`ToolName` = p_ItemName
      AND tap.`IsAllowed` = 1
      AND t.`IsEnabled` = 1
      AND s.`IsActive` = 1;
END //

-- 2. Procedure: Get allowed tools/items for given group names
DROP PROCEDURE IF EXISTS `sp_GetAllowedItemsForGroups` //
CREATE PROCEDURE `sp_GetAllowedItemsForGroups`(
    IN p_GroupNames TEXT
)
BEGIN
    SELECT DISTINCT 
        t.`ToolId`, 
        t.`ServerId`, 
        s.`CodeName` AS ServerCodeName, 
        t.`ToolName`, 
        t.`VaultSecretPath`,
        t.`SecretProvider`
    FROM `Tools` t
    INNER JOIN `McpServers` s ON t.`ServerId` = s.`ServerId`
    INNER JOIN `ToolAccessPolicies` tap ON t.`ToolId` = tap.`ToolId`
    INNER JOIN `AdGroups` g ON tap.`GroupId` = g.`GroupId`
    WHERE FIND_IN_SET(g.`GroupName`, p_GroupNames) > 0
      AND tap.`IsAllowed` = 1
      AND t.`IsEnabled` = 1
      AND s.`IsActive` = 1;
END //

-- 3. Procedure: Get Secret Path and Explicit SecretProvider for a Server
DROP PROCEDURE IF EXISTS `sp_GetServerSecrets` //
CREATE PROCEDURE `sp_GetServerSecrets`(
    IN p_ServerCodeName VARCHAR(100)
)
BEGIN
    SELECT 
        s.`ServerId`,
        s.`CodeName`,
        s.`SecretProvider` AS ServerSecretProvider,
        t.`ToolName`,
        t.`VaultSecretPath`,
        t.`SecretProvider` AS ToolSecretProvider
    FROM `McpServers` s
    LEFT JOIN `Tools` t ON s.`ServerId` = t.`ServerId`
    WHERE s.`CodeName` = p_ServerCodeName
      AND s.`IsActive` = 1;
END //

-- 4. Procedure: Save or Update Secret Provider Configuration
DROP PROCEDURE IF EXISTS `sp_SaveSecretProvider` //
CREATE PROCEDURE `sp_SaveSecretProvider`(
    IN p_ProviderName VARCHAR(50),
    IN p_DisplayName VARCHAR(100),
    IN p_EncryptedConfigJson LONGTEXT,
    IN p_IsEnabled TINYINT(1)
)
BEGIN
    INSERT INTO `SecretProviders` (`ProviderName`, `DisplayName`, `EncryptedConfigJson`, `IsEnabled`)
    VALUES (p_ProviderName, p_DisplayName, p_EncryptedConfigJson, p_IsEnabled)
    ON DUPLICATE KEY UPDATE
        `DisplayName` = p_DisplayName,
        `EncryptedConfigJson` = p_EncryptedConfigJson,
        `IsEnabled` = p_IsEnabled;
END //

-- 5. Procedure: Save or Update Identity/Auth Provider Configuration
DROP PROCEDURE IF EXISTS `sp_SaveAuthProvider` //
CREATE PROCEDURE `sp_SaveAuthProvider`(
    IN p_ProviderName VARCHAR(50),
    IN p_DisplayName VARCHAR(100),
    IN p_UserHeader VARCHAR(100),
    IN p_GroupsHeader VARCHAR(100),
    IN p_IsEnabled TINYINT(1)
)
BEGIN
    INSERT INTO `AuthProviderConfigs` (`ProviderName`, `DisplayName`, `UserHeader`, `GroupsHeader`, `IsEnabled`)
    VALUES (p_ProviderName, p_DisplayName, p_UserHeader, p_GroupsHeader, p_IsEnabled)
    ON DUPLICATE KEY UPDATE
        `DisplayName` = p_DisplayName,
        `UserHeader` = p_UserHeader,
        `GroupsHeader` = p_GroupsHeader,
        `IsEnabled` = p_IsEnabled;
END //

-- 6. Procedure: Insert Audit Log Entry
DROP PROCEDURE IF EXISTS `sp_InsertAuditLog` //
CREATE PROCEDURE `sp_InsertAuditLog`(
    IN p_RequestId VARCHAR(64),
    IN p_UserPrincipalName VARCHAR(256),
    IN p_UserSid VARCHAR(180),
    IN p_ServerCodeName VARCHAR(100),
    IN p_ItemName VARCHAR(150),
    IN p_RequestMethod VARCHAR(50),
    IN p_ExecutionTimeMs INT,
    IN p_StatusCode INT,
    IN p_RequestPayload LONGTEXT,
    IN p_ResponsePayload LONGTEXT,
    IN p_ErrorMessage LONGTEXT
)
BEGIN
    INSERT INTO `AuditLogs` (
        `RequestId`, `UserPrincipalName`, `UserSid`, `ServerCodeName`,
        `ItemName`, `RequestMethod`, `ExecutionTimeMs`, `StatusCode`,
        `RequestPayload`, `ResponsePayload`, `ErrorMessage`, `Timestamp`
    )
    VALUES (
        p_RequestId, p_UserPrincipalName, p_UserSid, p_ServerCodeName,
        p_ItemName, p_RequestMethod, p_ExecutionTimeMs, p_StatusCode,
        p_RequestPayload, p_ResponsePayload, p_ErrorMessage, NOW()
    );
END //

DELIMITER ;
