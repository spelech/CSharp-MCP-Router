-- ============================================================================
-- Enterprise MCP Gateway Stored Procedures (MySQL / MariaDB)
-- ============================================================================

USE `McpEnterpriseDb`;

DELIMITER //

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
        t.`VaultSecretPath`
    FROM `Tools` t
    INNER JOIN `McpServers` s ON t.`ServerId` = s.`ServerId`
    INNER JOIN `ToolAccessPolicies` tap ON t.`ToolId` = tap.`ToolId`
    INNER JOIN `AdGroups` g ON tap.`GroupId` = g.`GroupId`
    WHERE FIND_IN_SET(g.`GroupName`, p_GroupNames) > 0
      AND tap.`IsAllowed` = 1
      AND t.`IsEnabled` = 1
      AND s.`IsActive` = 1;
END //

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
