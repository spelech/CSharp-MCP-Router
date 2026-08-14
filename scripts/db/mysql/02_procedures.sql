-- ============================================================================
-- Enterprise MCP Gateway Stored Procedures (MySQL / MariaDB)
-- ============================================================================

USE `McpEnterpriseDb`;

DELIMITER //

-- 1. Procedure: Evaluate User Access for a specific Tool/Item and Group SIDs/Names
DROP PROCEDURE IF EXISTS `sp_EvaluateUserAccess` //
CREATE PROCEDURE `sp_EvaluateUserAccess`(
    IN p_GroupNames TEXT,
    IN p_ItemName VARCHAR(150),
    IN p_RequestMethod VARCHAR(50)
)
BEGIN
    DECLARE v_ServerId VARCHAR(100);
    DECLARE v_HasPolicies INT;
    DECLARE v_HasDeny INT;
    DECLARE v_HasAllow INT;

    SET v_ServerId = p_ItemName;
    IF LOCATE('__', p_ItemName) > 0 THEN
        SET v_ServerId = SUBSTRING(p_ItemName, 1, LOCATE('__', p_ItemName) - 1);
    END IF;

    -- Create temporary table for targets
    CREATE TEMPORARY TABLE IF NOT EXISTS temp_targets (TargetId VARCHAR(250));
    DELETE FROM temp_targets;

    INSERT INTO temp_targets VALUES
        (CONCAT('tool:', p_ItemName)),
        (CONCAT('prompt:', p_ItemName)),
        (CONCAT('resource:', p_ItemName)),
        (CONCAT('server:', v_ServerId));

    -- Create temporary table for groups
    CREATE TEMPORARY TABLE IF NOT EXISTS temp_groups (GroupName VARCHAR(256));
    DELETE FROM temp_groups;

    -- Custom split loop for CSV group names
    BEGIN
        DECLARE idx INT DEFAULT 1;
        DECLARE val TEXT;
        DECLARE comma_pos INT;

        split_loop: LOOP
            SET comma_pos = LOCATE(',', p_GroupNames, idx);
            IF comma_pos > 0 THEN
                SET val = SUBSTRING(p_GroupNames, idx, comma_pos - idx);
                INSERT INTO temp_groups VALUES (TRIM(val));
                SET idx = comma_pos + 1;
            ELSE
                SET val = SUBSTRING(p_GroupNames, idx);
                IF TRIM(val) != '' THEN
                    INSERT INTO temp_groups VALUES (TRIM(val));
                END IF;
                LEAVE split_loop;
            END IF;
        END LOOP split_loop;
    END;

    -- Check if any policies configured
    SELECT COUNT(*) INTO v_HasPolicies
    FROM `AccessPolicies`
    WHERE `TargetId` IN (SELECT TargetId FROM temp_targets);

    IF v_HasPolicies = 0 THEN
        SELECT 0 AS IsAllowed;
    ELSE
        -- Check explicit deny
        SELECT COUNT(*) INTO v_HasDeny
        FROM `AccessPolicies`
        WHERE `TargetId` IN (SELECT TargetId FROM temp_targets)
          AND `RequiredGroup` IN (SELECT GroupName FROM temp_groups)
          AND `IsAllowed` = 0;

        IF v_HasDeny > 0 THEN
            SELECT 0 AS IsAllowed;
        ELSE
            -- Check explicit allow
            SELECT COUNT(*) INTO v_HasAllow
            FROM `AccessPolicies`
            WHERE `TargetId` IN (SELECT TargetId FROM temp_targets)
              AND `RequiredGroup` IN (SELECT GroupName FROM temp_groups)
              AND `IsAllowed` = 1;

            IF v_HasAllow > 0 THEN
                SELECT 1 AS IsAllowed;
            ELSE
                SELECT 0 AS IsAllowed;
            END IF;
        END IF;
    END IF;
END //

-- 2. Procedure: Get allowed tools/items for given group names
DROP PROCEDURE IF EXISTS `sp_GetAllowedItemsForGroups` //
CREATE PROCEDURE `sp_GetAllowedItemsForGroups`(
    IN p_GroupNames TEXT
)
BEGIN
    CREATE TEMPORARY TABLE IF NOT EXISTS temp_allowed_groups (GroupName VARCHAR(256));
    DELETE FROM temp_allowed_groups;

    BEGIN
        DECLARE idx INT DEFAULT 1;
        DECLARE val TEXT;
        DECLARE comma_pos INT;

        split_loop: LOOP
            SET comma_pos = LOCATE(',', p_GroupNames, idx);
            IF comma_pos > 0 THEN
                SET val = SUBSTRING(p_GroupNames, idx, comma_pos - idx);
                INSERT INTO temp_allowed_groups VALUES (TRIM(val));
                SET idx = comma_pos + 1;
            ELSE
                SET val = SUBSTRING(p_GroupNames, idx);
                IF TRIM(val) != '' THEN
                    INSERT INTO temp_allowed_groups VALUES (TRIM(val));
                END IF;
                LEAVE split_loop;
            END IF;
        END LOOP split_loop;
    END;

    SELECT DISTINCT 
        t.`ToolId`, 
        t.`ServerId`, 
        s.`Id` AS ServerCodeName,
        t.`ToolName`, 
        t.`VaultSecretPath`,
        t.`SecretProvider`
    FROM `Tools` t
    INNER JOIN `Servers` s ON t.`ServerId` = s.`Id`
    INNER JOIN `ToolAccessPolicies` tap ON t.`ToolId` = tap.`ToolId`
    INNER JOIN `AdGroups` g ON tap.`GroupId` = g.`GroupId`
    WHERE g.`GroupName` IN (SELECT GroupName FROM temp_allowed_groups)
      AND tap.`IsAllowed` = 1
      AND t.`IsEnabled` = 1
      AND s.`Enabled` = 1;
END //

-- 3. Procedure: Get Secret Path and Explicit SecretProvider for a Server
DROP PROCEDURE IF EXISTS `sp_GetServerSecrets` //
CREATE PROCEDURE `sp_GetServerSecrets`(
    IN p_ServerCodeName VARCHAR(100)
)
BEGIN
    SELECT 
        s.`Id` AS ServerId,
        s.`Id` AS CodeName,
        s.`SecretProvider` AS ServerSecretProvider,
        t.`ToolName`,
        t.`VaultSecretPath`,
        t.`SecretProvider` AS ToolSecretProvider
    FROM `Servers` s
    LEFT JOIN `Tools` t ON s.`Id` = t.`ServerId`
    WHERE s.`Id` = p_ServerCodeName
      AND s.`Enabled` = 1;
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
    IN p_ConfigJson LONGTEXT,
    IN p_IsEnabled TINYINT(1)
)
BEGIN
    INSERT INTO `AuthProviderConfigs` (`ProviderName`, `DisplayName`, `UserHeader`, `GroupsHeader`, `ConfigJson`, `IsEnabled`)
    VALUES (p_ProviderName, p_DisplayName, p_UserHeader, p_GroupsHeader, p_ConfigJson, p_IsEnabled)
    ON DUPLICATE KEY UPDATE
        `DisplayName` = p_DisplayName,
        `UserHeader` = p_UserHeader,
        `GroupsHeader` = p_GroupsHeader,
        `ConfigJson` = p_ConfigJson,
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

-- 7. Procedure: Save or Update AppKey Configuration
DROP PROCEDURE IF EXISTS `sp_SaveAppKey` //
CREATE PROCEDURE `sp_SaveAppKey`(
    IN p_Id VARCHAR(100),
    IN p_Name VARCHAR(200),
    IN p_Username VARCHAR(256),
    IN p_KeyPrefix VARCHAR(50),
    IN p_EncryptedKey LONGTEXT,
    IN p_ScopesJson LONGTEXT,
    IN p_OwnerSid VARCHAR(200),
    IN p_ExpiresAt DATETIME
)
BEGIN
    INSERT INTO `AppKeys` (`Id`, `Name`, `Username`, `OwnerSid`, `KeyPrefix`, `EncryptedKey`, `ScopesJson`, `ExpiresAt`, `CreatedAt`)
    VALUES (p_Id, p_Name, p_Username, IFNULL(p_OwnerSid, ''), p_KeyPrefix, p_EncryptedKey, p_ScopesJson, p_ExpiresAt, NOW())
    ON DUPLICATE KEY UPDATE
        `Name` = p_Name,
        `Username` = p_Username,
        `OwnerSid` = IFNULL(p_OwnerSid, ''),
        `KeyPrefix` = p_KeyPrefix,
        `EncryptedKey` = p_EncryptedKey,
        `ScopesJson` = p_ScopesJson,
        `ExpiresAt` = p_ExpiresAt;
END //

-- 8. Procedure: Delete/Revoke AppKey
DROP PROCEDURE IF EXISTS `sp_DeleteAppKey` //
CREATE PROCEDURE `sp_DeleteAppKey`(
    IN p_Id VARCHAR(100)
)
BEGIN
    DELETE FROM `AppKeys` WHERE `Id` = p_Id;
END //

-- 9. Procedure: Get AppKeys
DROP PROCEDURE IF EXISTS `sp_GetAppKeys` //
CREATE PROCEDURE `sp_GetAppKeys`(
    IN p_Username VARCHAR(256)
)
BEGIN
    IF p_Username IS NULL THEN
        SELECT `Id`, `Name`, `Username`, `KeyPrefix`, `EncryptedKey`, `ScopesJson`, `ExpiresAt`, `CreatedAt`
        FROM `AppKeys`;
    END IF;
    IF p_Username IS NOT NULL THEN
        SELECT `Id`, `Name`, `Username`, `KeyPrefix`, `EncryptedKey`, `ScopesJson`, `ExpiresAt`, `CreatedAt`
        FROM `AppKeys`
        WHERE `Username` = p_Username;
    END IF;
END //

-- 10. Procedure: Insert Admin Audit Log Entry
DROP PROCEDURE IF EXISTS `sp_InsertAdminAuditLog` //
CREATE PROCEDURE `sp_InsertAdminAuditLog`(
    IN p_Id VARCHAR(50),
    IN p_Username VARCHAR(256),
    IN p_Action VARCHAR(100),
    IN p_Target VARCHAR(256),
    IN p_Details LONGTEXT,
    IN p_Success TINYINT(1),
    IN p_ErrorMessage LONGTEXT
)
BEGIN
    INSERT INTO `AdminAuditLogs` (
        `Id`, `Username`, `Action`, `Target`, `Details`, `Success`, `ErrorMessage`, `Timestamp`
    )
    VALUES (
        p_Id, p_Username, p_Action, p_Target, p_Details, p_Success, p_ErrorMessage, NOW()
    );
END //

DELIMITER ;
