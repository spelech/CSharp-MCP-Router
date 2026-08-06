import React from 'react';

interface ConsoleCardProps {
  consoleRequest: string;
  consoleResponse: string;
}

export const ConsoleCard: React.FC<ConsoleCardProps> = ({ consoleRequest, consoleResponse }) => {
  return (
    <div className="glass-card">
      <h2>
        <i className="fa-solid fa-terminal"></i> Execution Console
      </h2>
      <div className="payload-viewer">
        <div className="payload-block">
          <label>JSON-RPC Request</label>
          <pre className="code-block" id="jsonrpc-request">
            {consoleRequest}
          </pre>
        </div>
        <div className="payload-block">
          <label>JSON-RPC Response</label>
          <pre className="code-block" id="jsonrpc-response">
            {consoleResponse}
          </pre>
        </div>
      </div>
    </div>
  );
};
