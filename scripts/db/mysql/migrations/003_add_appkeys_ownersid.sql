-- Migration: Add OwnerSid to AppKeys table and update stored procedures
-- Target: MySQL / MariaDB

USE `McpEnterpriseDb`;

DELIMITER //

-- 1. Add OwnerSid to AppKeys table safely
DROP PROCEDURE IF EXISTS `_temp_add_ownersid_column` //
CREATE PROCEDURE `_temp_add_ownersid_column`()
BEGIN
    IF NOT EXISTS (
        SELECT * FROM information_schema.columns
        WHERE table_schema = 'McpEnterpriseDb'
          AND table_name = 'AppKeys'
          AND column_name = 'OwnerSid'
    ) THEN
        ALTER TABLE `AppKeys` ADD COLUMN `OwnerSid` VARCHAR(180) NULL;
    END IF;
END //

DELIMITER ;
CALL _temp_add_ownersid_column();
DROP PROCEDURE IF EXISTS _temp_add_ownersid_column;

DELIMITER //

-- 2. Update Procedure: sp_SaveAppKey
DROP PROCEDURE IF EXISTS `sp_SaveAppKey` //
CREATE PROCEDURE `sp_SaveAppKey`(
    IN p_Id VARCHAR(100),
    IN p_Name VARCHAR(200),
    IN p_Username VARCHAR(256),
    IN p_KeyPrefix VARCHAR(50),
    IN p_EncryptedKey LONGTEXT,
    IN p_ScopesJson LONGTEXT,
    IN p_ExpiresAt DATETIME,
    IN p_OwnerSid VARCHAR(180)
)
BEGIN
    INSERT INTO `AppKeys` (`Id`, `Name`, `Username`, `KeyPrefix`, `EncryptedKey`, `ScopesJson`, `ExpiresAt`, `OwnerSid`, `CreatedAt`)
    VALUES (p_Id, p_Name, p_Username, p_KeyPrefix, p_EncryptedKey, p_ScopesJson, p_ExpiresAt, p_OwnerSid, NOW())
    ON DUPLICATE KEY UPDATE
        `Name` = p_Name,
        `Username` = p_Username,
        `KeyPrefix` = p_KeyPrefix,
        `EncryptedKey` = p_EncryptedKey,
        `ScopesJson` = p_ScopesJson,
        `ExpiresAt` = p_ExpiresAt,
        `OwnerSid` = p_OwnerSid;
END //

-- 3. Update Procedure: sp_GetAppKeys
DROP PROCEDURE IF EXISTS `sp_GetAppKeys` //
CREATE PROCEDURE `sp_GetAppKeys`(
    IN p_Username VARCHAR(256)
)
BEGIN
    IF p_Username IS NULL OR p_Username = '' THEN
        SELECT `Id`, `Name`, `Username`, `KeyPrefix`, `EncryptedKey`, `ScopesJson`, `ExpiresAt`, `OwnerSid`, `CreatedAt`
        FROM `AppKeys`;
    ELSE
        SELECT `Id`, `Name`, `Username`, `KeyPrefix`, `EncryptedKey`, `ScopesJson`, `ExpiresAt`, `OwnerSid`, `CreatedAt`
        FROM `AppKeys`
        WHERE `Username` = p_Username;
    END IF;
END //

DELIMITER ;