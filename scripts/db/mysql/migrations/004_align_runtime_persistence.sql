-- Migration: Align Runtime Persistence (Servers, Tools.ServerId VARCHAR(100), EncryptedConfigJson, ConfigJson, OwnerSid)
-- Target: MySQL / MariaDB

USE `McpEnterpriseDb`;

DELIMITER //

-- 1. Migrate McpServers to Servers table
DROP PROCEDURE IF EXISTS `_temp_migrate_mcpservers` //
CREATE PROCEDURE `_temp_migrate_mcpservers`()
BEGIN
    DECLARE v_mcp_exists INT DEFAULT 0;
    DECLARE v_servers_exists INT DEFAULT 0;
    DECLARE v_has_id INT DEFAULT 0;

    SELECT COUNT(*) INTO v_mcp_exists
    FROM information_schema.tables
    WHERE table_schema = DATABASE() AND table_name = 'McpServers';

    IF v_mcp_exists > 0 THEN
        SELECT COUNT(*) INTO v_servers_exists
        FROM information_schema.tables
        WHERE table_schema = DATABASE() AND table_name = 'Servers';

        IF v_servers_exists = 0 THEN
            CREATE TABLE `Servers` (
                `Id`                VARCHAR(100) PRIMARY KEY,
                `DisplayName`       VARCHAR(200) NOT NULL,
                `Url`               VARCHAR(500) NOT NULL,
                `Enabled`           TINYINT(1) NOT NULL DEFAULT 1,
                `Hidden`            TINYINT(1) NOT NULL DEFAULT 0,
                `Type`              VARCHAR(20) NOT NULL DEFAULT 'sse',
                `SecretProvider`    VARCHAR(50) NOT NULL DEFAULT 'None',
                `SecretItemKey`     VARCHAR(100) NULL,
                `SecretMount`       VARCHAR(100) NULL,
                `SecretPath`        VARCHAR(250) NULL,
                `SecretField`       VARCHAR(100) NULL,
                `AuthShape`         VARCHAR(20) NOT NULL DEFAULT 'bearer',
                `CustomHeaderName`  VARCHAR(100) NULL,
                `Categories`        LONGTEXT NOT NULL,
                `ApiKey`            LONGTEXT NULL,
                `HeadersJson`       LONGTEXT NULL,
                `AutoDiscovered`    TINYINT(1) NOT NULL DEFAULT 0
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        END IF;

        SELECT COUNT(*) INTO v_has_id
        FROM information_schema.columns
        WHERE table_schema = DATABASE() AND table_name = 'McpServers' AND column_name = 'Id';

        IF v_has_id > 0 THEN
            INSERT IGNORE INTO `Servers` (`Id`, `DisplayName`, `Url`, `Enabled`, `Hidden`, `Type`, `SecretProvider`, `SecretItemKey`, `SecretMount`, `SecretPath`, `SecretField`, `AuthShape`, `CustomHeaderName`, `Categories`, `ApiKey`, `HeadersJson`, `AutoDiscovered`)
            SELECT `Id`, `DisplayName`, `Url`, `Enabled`, `Hidden`, `Type`, `SecretProvider`, `SecretItemKey`, `SecretMount`, `SecretPath`, `SecretField`, `AuthShape`, `CustomHeaderName`, `Categories`, `ApiKey`, `HeadersJson`, `AutoDiscovered`
            FROM `McpServers`;
        ELSE
            INSERT IGNORE INTO `Servers` (`Id`, `DisplayName`, `Url`, `Enabled`, `Hidden`, `Type`, `SecretProvider`, `Categories`)
            SELECT `CodeName`, `DisplayName`, `Url`, IFNULL(`IsActive`, 1), 0, 'sse', IFNULL(`SecretProvider`, 'None'), '[]'
            FROM `McpServers`;
        END IF;
    END IF;
END //

DELIMITER ;
CALL _temp_migrate_mcpservers();
DROP PROCEDURE IF EXISTS _temp_migrate_mcpservers;

DELIMITER //

-- 2. Migrate Tools.ServerId to VARCHAR(100)
DROP PROCEDURE IF EXISTS `_temp_migrate_tools_serverid` //
CREATE PROCEDURE `_temp_migrate_tools_serverid`()
BEGIN
    DECLARE v_tools_exists INT DEFAULT 0;
    DECLARE v_serverid_type VARCHAR(50);

    SELECT COUNT(*) INTO v_tools_exists
    FROM information_schema.tables
    WHERE table_schema = DATABASE() AND table_name = 'Tools';

    IF v_tools_exists > 0 THEN
        SELECT DATA_TYPE INTO v_serverid_type
        FROM information_schema.columns
        WHERE table_schema = DATABASE() AND table_name = 'Tools' AND column_name = 'ServerId';

        IF v_serverid_type = 'int' OR v_serverid_type = 'bigint' OR v_serverid_type = 'smallint' THEN
            ALTER TABLE `Tools` MODIFY COLUMN `ServerId` VARCHAR(100) NOT NULL;
        END IF;
    END IF;
END //

DELIMITER ;
CALL _temp_migrate_tools_serverid();
DROP PROCEDURE IF EXISTS _temp_migrate_tools_serverid;

DELIMITER //

-- 3. Migrate SecretProviders.ConfigJson to EncryptedConfigJson
DROP PROCEDURE IF EXISTS `_temp_migrate_secretproviders_configjson` //
CREATE PROCEDURE `_temp_migrate_secretproviders_configjson`()
BEGIN
    DECLARE v_sp_exists INT DEFAULT 0;
    DECLARE v_enc_exists INT DEFAULT 0;
    DECLARE v_cfg_exists INT DEFAULT 0;

    SELECT COUNT(*) INTO v_sp_exists
    FROM information_schema.tables
    WHERE table_schema = DATABASE() AND table_name = 'SecretProviders';

    IF v_sp_exists > 0 THEN
        SELECT COUNT(*) INTO v_enc_exists
        FROM information_schema.columns
        WHERE table_schema = DATABASE() AND table_name = 'SecretProviders' AND column_name = 'EncryptedConfigJson';

        IF v_enc_exists = 0 THEN
            SELECT COUNT(*) INTO v_cfg_exists
            FROM information_schema.columns
            WHERE table_schema = DATABASE() AND table_name = 'SecretProviders' AND column_name = 'ConfigJson';

            IF v_cfg_exists > 0 THEN
                ALTER TABLE `SecretProviders` ADD COLUMN `EncryptedConfigJson` LONGTEXT NULL;
                UPDATE `SecretProviders` SET `EncryptedConfigJson` = `ConfigJson` WHERE `EncryptedConfigJson` IS NULL;
            ELSE
                ALTER TABLE `SecretProviders` ADD COLUMN `EncryptedConfigJson` LONGTEXT NULL;
            END IF;
        END IF;
    END IF;
END //

DELIMITER ;
CALL _temp_migrate_secretproviders_configjson();
DROP PROCEDURE IF EXISTS _temp_migrate_secretproviders_configjson;

DELIMITER //

-- 4. Migrate AuthProviderConfigs.ConfigJson to EncryptedConfigJson
DROP PROCEDURE IF EXISTS `_temp_migrate_authproviders_configjson` //
CREATE PROCEDURE `_temp_migrate_authproviders_configjson`()
BEGIN
    DECLARE v_ap_exists INT DEFAULT 0;
    DECLARE v_enc_exists INT DEFAULT 0;
    DECLARE v_cfg_exists INT DEFAULT 0;

    SELECT COUNT(*) INTO v_ap_exists
    FROM information_schema.tables
    WHERE table_schema = DATABASE() AND table_name = 'AuthProviderConfigs';

    IF v_ap_exists > 0 THEN
        SELECT COUNT(*) INTO v_enc_exists
        FROM information_schema.columns
        WHERE table_schema = DATABASE() AND table_name = 'AuthProviderConfigs' AND column_name = 'EncryptedConfigJson';

        IF v_enc_exists = 0 THEN
            SELECT COUNT(*) INTO v_cfg_exists
            FROM information_schema.columns
            WHERE table_schema = DATABASE() AND table_name = 'AuthProviderConfigs' AND column_name = 'ConfigJson';

            IF v_cfg_exists > 0 THEN
                ALTER TABLE `AuthProviderConfigs` ADD COLUMN `EncryptedConfigJson` LONGTEXT NULL;
                UPDATE `AuthProviderConfigs` SET `EncryptedConfigJson` = `ConfigJson` WHERE `EncryptedConfigJson` IS NULL;
            ELSE
                ALTER TABLE `AuthProviderConfigs` ADD COLUMN `EncryptedConfigJson` LONGTEXT NULL;
            END IF;
        END IF;
    END IF;
END //

DELIMITER ;
CALL _temp_migrate_authproviders_configjson();
DROP PROCEDURE IF EXISTS _temp_migrate_authproviders_configjson;

DELIMITER //

-- 5. Migrate AppKeys.OwnerSid
DROP PROCEDURE IF EXISTS `_temp_migrate_appkeys_ownersid` //
CREATE PROCEDURE `_temp_migrate_appkeys_ownersid`()
BEGIN
    DECLARE v_ak_exists INT DEFAULT 0;
    DECLARE v_sid_exists INT DEFAULT 0;

    SELECT COUNT(*) INTO v_ak_exists
    FROM information_schema.tables
    WHERE table_schema = DATABASE() AND table_name = 'AppKeys';

    IF v_ak_exists > 0 THEN
        SELECT COUNT(*) INTO v_sid_exists
        FROM information_schema.columns
        WHERE table_schema = DATABASE() AND table_name = 'AppKeys' AND column_name = 'OwnerSid';

        IF v_sid_exists = 0 THEN
            ALTER TABLE `AppKeys` ADD COLUMN `OwnerSid` VARCHAR(200) NOT NULL DEFAULT '';
        END IF;
    END IF;
END //

DELIMITER ;
CALL _temp_migrate_appkeys_ownersid();
DROP PROCEDURE IF EXISTS _temp_migrate_appkeys_ownersid;
