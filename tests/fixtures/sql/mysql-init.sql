-- MySQL Test Database Initialization Script
CREATE DATABASE IF NOT EXISTS mcp_router_test CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

GRANT ALL PRIVILEGES ON mcp_router_test.* TO 'mcp_user'@'%';
GRANT ALL PRIVILEGES ON mcp_router_test.* TO 'root'@'%';
FLUSH PRIVILEGES;
