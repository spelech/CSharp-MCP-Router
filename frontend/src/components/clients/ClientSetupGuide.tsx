import React, { useState } from 'react';

export const ClientSetupGuide: React.FC = () => {
  const [selectedClient, setSelectedClient] = useState<'claude' | 'cursor' | 'cline' | 'generic'>('cursor');

  const renderConfig = () => {
    switch (selectedClient) {
      case 'claude':
        return (
          <div>
            <p>
              Add the router to your Claude Desktop configuration file (<code>claude_desktop_config.json</code>):
            </p>
            <pre>
              {JSON.stringify(
                {
                  mcpServers: {
                    "mcp-router": {
                      url: "http://10.0.0.10:8026/sse",
                      headers: {
                        "X-App-Key": "mcp_live_YOUR_APP_KEY_HERE"
                      }
                    }
                  }
                },
                null,
                2
              )}
            </pre>
          </div>
        );
      case 'cursor':
        return (
          <div>
            <p>
              In Cursor IDE Settings &gt; Features &gt; MCP Servers, click "Add New MCP Server":
            </p>
            <ul>
              <li><strong>Name:</strong> <code>mcp-router</code></li>
              <li><strong>Type:</strong> <code>sse</code></li>
              <li><strong>Server URL:</strong> <code>http://10.0.0.10:8026/sse</code></li>
              <li><strong>Headers:</strong> <code>X-App-Key: mcp_live_YOUR_APP_KEY_HERE</code></li>
            </ul>
          </div>
        );
      case 'cline':
        return (
          <div>
            <p>
              In VSCode Cline / Roo-Code MCP Settings (<code>cline_mcp_settings.json</code>):
            </p>
            <pre>
              {JSON.stringify(
                {
                  mcpServers: {
                    "mcp-router": {
                      url: "http://10.0.0.10:8026/sse",
                      type: "sse",
                      headers: {
                        "X-App-Key": "mcp_live_YOUR_APP_KEY_HERE"
                      }
                    }
                  }
                },
                null,
                2
              )}
            </pre>
          </div>
        );
      case 'generic':
      default:
        return (
          <div>
            <p>
              Direct connection via SSE transport:
            </p>
            <ul>
              <li><strong>SSE Endpoint:</strong> <code>http://10.0.0.10:8026/sse</code></li>
              <li><strong>HTTP Message Endpoint:</strong> <code>http://10.0.0.10:8026/messages?sessionId=&lt;session_id&gt;</code></li>
              <li><strong>Authentication:</strong> Include header <code>X-App-Key: &lt;your_key&gt;</code> or <code>Authorization: Bearer &lt;token&gt;</code></li>
            </ul>
          </div>
        );
    }
  };

  return (
    <div className="glass-card guide-card">
      <h2>
        <i className="fa-solid fa-book"></i> Client Connection Guide
      </h2>
      <p style={{ color: 'var(--text-muted)', fontSize: '13px', marginBottom: '15px' }}>
        Connect your preferred AI client or IDE extension to the unified MCP Router gateway.
      </p>

      <div className="tester-tabs" style={{ marginBottom: '15px' }}>
        <button
          type="button"
          className={`tester-tab-btn ${selectedClient === 'cursor' ? 'active' : ''}`}
          onClick={() => setSelectedClient('cursor')}
        >
          <i className="fa-solid fa-code"></i> Cursor IDE
        </button>
        <button
          type="button"
          className={`tester-tab-btn ${selectedClient === 'claude' ? 'active' : ''}`}
          onClick={() => setSelectedClient('claude')}
        >
          <i className="fa-solid fa-brain"></i> Claude Desktop
        </button>
        <button
          type="button"
          className={`tester-tab-btn ${selectedClient === 'cline' ? 'active' : ''}`}
          onClick={() => setSelectedClient('cline')}
        >
          <i className="fa-solid fa-terminal"></i> Cline / Roo
        </button>
        <button
          type="button"
          className={`tester-tab-btn ${selectedClient === 'generic' ? 'active' : ''}`}
          onClick={() => setSelectedClient('generic')}
        >
          <i className="fa-solid fa-network-wired"></i> Generic SSE
        </button>
      </div>

      <div className="guide-content" style={{ background: 'rgba(0,0,0,0.2)', padding: '15px', borderRadius: '8px', border: '1px solid var(--border-color)', fontSize: '13px' }}>
        {renderConfig()}
      </div>
    </div>
  );
};
