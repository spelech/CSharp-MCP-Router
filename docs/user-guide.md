# 📖 Model Context Gateway (MCG) User Guide

Welcome to the user guide for the Model Context Gateway (MCG). This guide helps you manage your user profile, create and manage application keys (AppKeys), and utilize the Interactive Test Bench for testing backend MCP tools.

## Managing Your Profile

Users can view and manage their personal details through the dashboard. 

### Viewing Quotas
Your profile defines limits on the number of Application Keys you are allowed to create. You can check your quotas in the dashboard, which displays:
- **Global Maximum Keys**: The absolute limit for active AppKeys across the entire router instance.
- **User Maximum Keys**: The limit for active AppKeys per individual user.
- **Total Active Keys**: The number of active AppKeys currently in the system.
- **Your Active Keys**: The number of AppKeys you have currently provisioned.

## Managing App Keys

Application Keys (AppKeys) act as credentials for authenticating with Model Context Gateway (MCG). You can manage your AppKeys under the credentials section of your dashboard.

### Creating an App Key
1. Click on **Create App Key**.
2. **Name**: Provide a descriptive name for the key.
3. **Scopes**: Define what the key is allowed to access (e.g., `all` or specific categories like `category:database`).
4. **Expiration**: Optionally define the number of days until the key expires.
5. The generated **Client ID** and **Client Secret** (Plaintext Key) will be displayed. 
   > [!WARNING]
   > The Plaintext Key is only shown once at creation. Store it securely!

### Revoking an App Key
If an AppKey is compromised or no longer needed, you should revoke it immediately.
1. Locate the AppKey in your list.
2. Click **Revoke**. The key will be immediately invalidated and can no longer be used for authentication.

## Interactive Test Bench

The Interactive Test Bench allows you to test tool calls against backend MCP servers directly through the router's UI or by calling the router's test endpoints.

### Using the Test Bench
1. Select a connected backend server from the server list.
2. The UI will automatically fetch and display the available tools provided by the backend server.
3. Select a tool to test.
4. Input the required JSON argument payload for the tool.
5. Click **Execute** or **Call Tool**. The router will handle the protocol handshake and forward your request to the backend server, displaying the result directly in the test bench interface.

> [!TIP]
> The test bench automatically handles protocol version negotiation (falling back to legacy versions if necessary) and handles the lifecycle of the connection for the duration of the test.
