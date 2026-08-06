import React from 'react';

interface PromptItem {
  name: string;
  description: string;
  arguments?: {
    name: string;
    description?: string;
    required?: boolean;
  }[];
}

interface PromptTesterCardProps {
  prompts: PromptItem[];
  selectedServer: string;
  selectedPromptName: string;
  promptArguments: Record<string, string>;
  onServerChange: (srv: string) => void;
  onPromptChange: (name: string) => void;
  onArgChange: (name: string, val: string) => void;
  onSubmit: (e: React.FormEvent) => void;
}

export const PromptTesterCard: React.FC<PromptTesterCardProps> = ({
  prompts,
  selectedServer,
  selectedPromptName,
  promptArguments,
  onServerChange,
  onPromptChange,
  onArgChange,
  onSubmit,
}) => {
  const getPromptServers = () => {
    const servers = new Set<string>();
    servers.add('router');
    prompts.forEach((p) => {
      const parts = p.name.split('__');
      if (parts.length > 1 && parts[0].startsWith('router')) {
        servers.add('router');
      } else if (parts.length > 1) {
        servers.add(parts[0]);
      } else {
        servers.add('router');
      }
    });
    return Array.from(servers).sort();
  };

  const getFilteredPrompts = () => {
    return prompts
      .filter((p) => {
        if (selectedServer === 'router') {
          return !p.name.includes('__') || p.name.startsWith('router__');
        }
        return p.name.startsWith(selectedServer + '__');
      })
      .sort((a, b) => a.name.localeCompare(b.name));
  };

  const currentPrompt = prompts.find((p) => p.name === selectedPromptName);

  return (
    <div className="glass-card">
      <h2>
        <i className="fa-solid fa-comments"></i> Interactive Prompt Tester
      </h2>
      <form onSubmit={onSubmit}>
        <div className="form-row">
          <div className="form-group">
            <label htmlFor="tester-prompt-server">Server</label>
            <select
              id="tester-prompt-server"
              value={selectedServer}
              onChange={(e) => onServerChange(e.target.value)}
              required
            >
              <option value="">-- Choose Server --</option>
              {getPromptServers().map((srv) => (
                <option key={srv} value={srv}>
                  {srv === 'router' ? 'Built-in Meta Workflows (router)' : `${srv.toUpperCase()} Server`}
                </option>
              ))}
            </select>
          </div>
          <div className="form-group">
            <label htmlFor="tester-prompt-name">Prompt</label>
            <select
              id="tester-prompt-name"
              value={selectedPromptName}
              onChange={(e) => onPromptChange(e.target.value)}
              required
            >
              <option value="">-- Choose Prompt --</option>
              {getFilteredPrompts().map((p) => {
                const display = p.name.includes('__') ? p.name.split('__')[1] : p.name;
                return (
                  <option key={p.name} value={p.name}>
                    {display}
                  </option>
                );
              })}
            </select>
          </div>
        </div>

        <div id="prompt-dynamic-fields" style={{ marginTop: '15px' }}>
          {selectedPromptName && currentPrompt ? (
            renderPromptFields(currentPrompt, promptArguments, onArgChange)
          ) : (
            <div className="empty-state">Select a prompt to generate arguments.</div>
          )}
        </div>

        <div style={{ marginTop: '20px' }}>
          <button type="submit" className="btn btn-primary" disabled={!selectedPromptName}>
            <i className="fa-solid fa-play"></i> Get Prompt Messages
          </button>
        </div>
      </form>
    </div>
  );
};

const renderPromptFields = (
  prompt: PromptItem,
  args: Record<string, string>,
  onChange: (name: string, val: string) => void
) => {
  if (!prompt.arguments || prompt.arguments.length === 0) {
    return <div className="empty-state">This prompt takes no arguments.</div>;
  }

  return prompt.arguments.map((arg) => {
    const reqText = arg.required ? <span style={{ color: 'var(--status-offline)' }}>*</span> : null;
    return (
      <div key={arg.name} className="param-field">
        <label>
          {arg.name} {reqText}
        </label>
        <input
          type="text"
          value={args[arg.name] || ''}
          onChange={(e) => onChange(arg.name, e.target.value)}
          required={arg.required}
        />
        {arg.description && <div className="field-desc">{arg.description}</div>}
      </div>
    );
  });
};
