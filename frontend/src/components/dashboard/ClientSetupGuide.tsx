import React, { useState } from 'react';
import { useServerStore } from '../../stores/useServerStore';

export const ClientSetupGuide: React.FC = () => {
  const { servers } = useServerStore();
  const [targetEndpoint, setTargetEndpoint] = useState<string>('meta');
  const [clientApp, setClientApp] = useState<'claude' | 'cursor' | 'vscode' | 'sdk'>('claude');
  const [customOrigin, setCustomOrigin] = useState<string>(typeof window !== 'undefined' ? window.location.origin : 'http://localhost:8080');
  const [includeAppKey, setIncludeAppKey] = useState<boolean>(false);
  const [copied, setCopied] = useState<boolean>(false);

  // Extract unique categories from registered servers
  const categories = Array.from(
    new Set(
      servers
        .flatMap((s) => s.categories || [])
        .filter(Boolean)
    )
  ).sort();

  const getTargetUrl = (): string => {
    const host = (customOrigin || 'http://localhost:8080').replace(/\/+$/, '');
    if (targetEndpoint === 'meta') {
      return `${host}/sse?meta=true`;
    }
    if (targetEndpoint.startsWith('server:')) {
      const serverId = targetEndpoint.slice(7);
      return `${host}/${serverId}`;
    }
    if (targetEndpoint.startsWith('category:')) {
      const cat = targetEndpoint.slice(9).toLowerCase();
      return `${host}/${cat}`;
    }
    return `${host}/sse?meta=true`;
  };

  const getGuideText = (): string => {
    const targetUrl = getTargetUrl();
    const appKeyHeader = includeAppKey ? { 'X-App-Key': 'YOUR_APP_KEY_HERE' } : undefined;

    if (clientApp === 'claude') {
      const config: any = {
        mcpServers: {
          'mcp-router': {
            command: 'npx',
            args: ['-y', '@modelcontextprotocol/client-sse', targetUrl],
          },
        },
      };
      if (includeAppKey) {
        config.mcpServers['mcp-router'].env = { X_APP_KEY: 'YOUR_APP_KEY_HERE' };
      }
      return JSON.stringify(config, null, 2);
    }

    if (clientApp === 'cursor') {
      const config: any = {
        mcpServers: {
          'mcp-router': {
            url: targetUrl,
          },
        },
      };
      if (appKeyHeader) {
        config.mcpServers['mcp-router'].headers = appKeyHeader;
      }
      return JSON.stringify(config, null, 2);
    }

    if (clientApp === 'vscode') {
      const config: any = {
        mcpServers: {
          'mcp-router': {
            type: 'sse',
            url: targetUrl,
          },
        },
      };
      if (appKeyHeader) {
        config.mcpServers['mcp-router'].headers = appKeyHeader;
      }
      return JSON.stringify(config, null, 2);
    }

    // TypeScript / Node.js SDK
    const headersStr = includeAppKey
      ? `,\n    requestInit: {\n      headers: {\n        "X-App-Key": "YOUR_APP_KEY_HERE"\n      }\n    }`
      : '';

    return `import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { SSEClientTransport } from "@modelcontextprotocol/sdk/client/sse.js";

const transport = new SSEClientTransport(
  new URL("${targetUrl}")${headersStr}
);

const client = new Client(
  { name: "mcp-client-agent", version: "1.0.0" },
  { capabilities: {} }
);

await client.connect(transport);`;
  };

  const handleCopyGuide = () => {
    navigator.clipboard.writeText(getGuideText()).then(() => {
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    });
  };

  return (
    <div className="glass-card dcr-card" style={{ marginTop: '20px' }}>
      <h2>
        <i className="fa-solid fa-circle-info"></i> MCP Client Setup Guide
      </h2>
      <p style={{ fontSize: '12px', color: 'var(--text-muted)', marginBottom: '15px' }}>
        Dynamically generate integration configurations targeting unified Meta-Mode, individual backend servers, or server categories across popular MCP client applications.
      </p>

      {/* Controls toolbar */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
          gap: '12px',
          marginBottom: '15px',
          background: 'rgba(255, 255, 255, 0.02)',
          padding: '12px',
          borderRadius: '8px',
          border: '1px solid rgba(255, 255, 255, 0.04)',
        }}
      >
        {/* Target Server / Endpoint Selector */}
        <div>
          <label style={{ display: 'block', fontSize: '11px', fontWeight: 600, color: 'var(--text-muted)', marginBottom: '4px' }}>
            <i className="fa-solid fa-server"></i> Target Route / Server:
          </label>
          <select
            className="form-control form-control-sm"
            value={targetEndpoint}
            onChange={(e) => setTargetEndpoint(e.target.value)}
            style={{ width: '100%', fontSize: '12px' }}
          >
            <optgroup label="Unified Gateway">
              <option value="meta">🌐 Meta-Mode Gateway (/sse?meta=true)</option>
            </optgroup>
            {servers.length > 0 && (
              <optgroup label="Individual MCP Servers">
                {servers.map((server) => (
                  <option key={server.id} value={`server:${server.id}`}>
                    🎯 {server.displayName || server.id} (/{server.id})
                  </option>
                ))}
              </optgroup>
            )}
            {categories.length > 0 && (
              <optgroup label="Server Categories">
                {categories.map((cat) => (
                  <option key={cat} value={`category:${cat}`}>
                    📁 {cat} Servers (/{cat.toLowerCase()})
                  </option>
                ))}
              </optgroup>
            )}
          </select>
        </div>

        {/* Client Application Selector */}
        <div>
          <label style={{ display: 'block', fontSize: '11px', fontWeight: 600, color: 'var(--text-muted)', marginBottom: '4px' }}>
            <i className="fa-solid fa-laptop-code"></i> Client Application:
          </label>
          <select
            className="form-control form-control-sm"
            value={clientApp}
            onChange={(e) => setClientApp(e.target.value as any)}
            style={{ width: '100%', fontSize: '12px' }}
          >
            <option value="claude">Claude Desktop (claude_desktop_config.json)</option>
            <option value="cursor">Cursor IDE (.cursor/mcp.json)</option>
            <option value="vscode">VS Code / Cline / Roo Code (mcpSettings.json)</option>
            <option value="sdk">TypeScript / Node.js SDK (Code Snippet)</option>
          </select>
        </div>

        {/* Host Origin Override */}
        <div>
          <label style={{ display: 'block', fontSize: '11px', fontWeight: 600, color: 'var(--text-muted)', marginBottom: '4px' }}>
            <i className="fa-solid fa-globe"></i> Router Host Origin:
          </label>
          <input
            type="text"
            className="form-control form-control-sm"
            value={customOrigin}
            onChange={(e) => setCustomOrigin(e.target.value)}
            placeholder="http://localhost:8080"
            style={{ width: '100%', fontSize: '12px' }}
          />
        </div>
      </div>

      {/* App Key Authorization Toggle */}
      <div style={{ marginBottom: '12px', display: 'flex', alignItems: 'center', gap: '8px' }}>
        <input
          type="checkbox"
          id="toggle-include-app-key"
          checked={includeAppKey}
          onChange={(e) => setIncludeAppKey(e.target.checked)}
          style={{ cursor: 'pointer' }}
        />
        <label htmlFor="toggle-include-app-key" style={{ fontSize: '12px', cursor: 'pointer', color: 'var(--text-main)', margin: 0 }}>
          Include <code>X-App-Key</code> authorization header in configuration
        </label>
      </div>

      {/* Code Snippet Display */}
      <div className="payload-viewer" style={{ marginTop: '10px' }}>
        <pre className="code-block" style={{ maxHeight: '240px', fontSize: '12px', overflowX: 'auto' }}>
          {getGuideText()}
        </pre>
      </div>

      <button
        type="button"
        className="btn btn-secondary btn-sm"
        id="btn-copy-client-guide"
        style={{ marginTop: '10px', width: '100%' }}
        onClick={handleCopyGuide}
      >
        {copied ? (
          <>
            <i className="fa-solid fa-check"></i> Copied to Clipboard!
          </>
        ) : (
          <>
            <i className="fa-solid fa-copy"></i> Copy Configuration
          </>
        )}
      </button>
    </div>
  );
};

