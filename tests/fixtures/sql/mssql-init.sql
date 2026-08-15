-- MSSQL Server Test Database Initialization Script
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'mcp_router_test')
BEGIN
    CREATE DATABASE [mcp_router_test];
END
GO
