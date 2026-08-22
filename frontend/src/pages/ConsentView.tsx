import React, { useEffect, useState } from 'react';

export const ConsentView: React.FC = () => {
  const [clientName, setClientName] = useState('');
  const searchParams = new URLSearchParams(window.location.search);

  useEffect(() => {
    setClientName(searchParams.get('client_name') || searchParams.get('client_id') || 'Unknown Client');
  }, []);

  return (
    <div className="card" style={{ maxWidth: 400, margin: '100px auto', textAlign: 'center' }}>
      <h1>Authorize Access</h1>
      <p>The application <span className="highlight">{clientName}</span> is requesting access to your MCP isolated backend resources.</p>
      <form method="post" action={`/connect/authorize${window.location.search}`}>
        <div className="button-group" style={{ display: 'flex', gap: 12, marginTop: 32 }}>
          <button type="submit" name="submit.Deny" value="Deny" className="btn btn-secondary" style={{ flex: 1 }}>Cancel</button>
          <button type="submit" name="submit.Accept" value="Accept" className="btn btn-primary" style={{ flex: 1 }}>Authorize</button>
        </div>
      </form>
    </div>
  );
};
