import React, { useState } from 'react';

export const ClientSetupGuide: React.FC = () => {
  const [guideTab, setGuideTab] = useState<'claude' | 'cursor'>('claude');
  const [copied, setCopied] = useState(false);

  const getGuideText = () => {
    const origin = window.location.origin;
    if (guideTab === 'claude') {
      return JSON.stringify({
        mcpServers: {
          "mcp-router": {
            command: "npx",
            args: [
              "-y",
              "@modelcontextprotocol/client-sse",
              `${origin}/sse?meta=true`
            ]
          }
        }
      }, null, 2);
    }
    return `Type: SSE\nURL: ${origin}/sse?meta=true\n\nOr JSON client integration configuration block:\n{\n  "mcpServers": {\n    "mcp-router": {\n      "command": "npx",\n      "args": [\n        "-y",\n        "@modelcontextprotocol/client-sse",\n        "${origin}/sse?meta=true"\n      ]\n    }\n  }\n}`;
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
        Copy the configuration below to register this gateway as an SSE server in your favorite AI clients.
      </p>

      <div className="tester-tabs" style={{ marginBottom: '10px' }}>
        <button
          type="button"
          className={`tester-tab-btn client-guide-tab ${guideTab === 'claude' ? 'active' : ''}`}
          onClick={() => setGuideTab('claude')}
        >
          Claude Desktop
        </button>
        <button
          type="button"
          className={`tester-tab-btn client-guide-tab ${guideTab === 'cursor' ? 'active' : ''}`}
          onClick={() => setGuideTab('cursor')}
        >
          Cursor / VSCode
        </button>
      </div>

      <div className="payload-viewer" style={{ marginTop: '10px' }}>
        <pre className="code-block" style={{ maxHeight: '200px', fontSize: '12px' }}>
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
            <i className="fa-solid fa-check"></i> Copied!
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
