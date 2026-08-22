# MCP Router Admin Guide

Welcome to the MCP Router Administration Guide. This document details the administrative procedures for managing backend server connections, configuring Role-Based Access Control (RBAC) policies, and managing secret and authentication providers.

## Server Connections

The MCP Router acts as a gateway connecting to multiple backend MCP servers. As an administrator, you configure and maintain these connections.

### Managing Servers
Servers can be managed via the Admin Dashboard or directly via the Admin MCP Server's tools.

- **Adding a Server**: Specify a Display Name, connection URL, and the transport protocol type (`sse`, `http`, `streamable`, `stdio`, or `custom`).
- **Authentication**: You can attach API keys, define custom headers, or integrate with Secret Providers (like Vault) to securely inject credentials at runtime without exposing them in the configuration.
- **Enabling/Disabling**: Servers can be temporarily disabled via the `toggle` feature, completely suspending routing to that backend.
- **Reconnecting**: If a backend server becomes unresponsive, you can trigger a manual reconnection. The Router will probe the server's health and refresh active sessions.

## Access Control & Policies

The MCP Router supports granular Role-Based Access Control (RBAC) to ensure that only authorized clients and users can invoke specific tools or access specific servers.

### Managing Policies
Policies determine whether a specific identity is allowed or denied access to a target.

- **Targets**: A policy targets a specific resource, which can be an entire backend Server ID, a specific Tool name, or a wildcard (`*`) to match any target.
- **Required Group**: Specify the required role, group name, or Security Identifier (SID) that the caller must possess.
- **Allow/Deny**: Policies can explicitly allow or deny access. A wildcard deny (`*`) cannot be created through standard tools as it would trigger a global lockout.

### Group Mappings
If you integrate with an External Identity Provider (like Azure AD or Okta), you can use Group Mappings to map external groups or SIDs to internal Router roles.

## Settings & Diagnostics

Administrators have access to a suite of system tools to monitor the Router's health and configure global settings.

- **Diagnostics**: View runtime metrics, active session counts, memory usage, and handle counts.
- **Audit Logs**: The router maintains an audit trail of all administrative actions and policy evaluations. You can query these logs by user, server, or timeframe.
- **Settings**: Configure global parameters, such as the maximum number of active AppKeys globally or per user, and manage the active Embedding Model configurations for semantic routing.
