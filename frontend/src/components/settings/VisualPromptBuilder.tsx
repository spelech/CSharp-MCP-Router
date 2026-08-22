import React from 'react';

export interface ArgBuilderItem {
  id: string;
  name: string;
  description: string;
  required: boolean;
}

export interface MsgBuilderItem {
  id: string;
  role: 'user' | 'assistant';
  text: string;
}

interface VisualPromptBuilderProps {
  promptDesc: string;
  setPromptDesc: (val: string) => void;
  builderArgs: ArgBuilderItem[];
  setBuilderArgs: React.Dispatch<React.SetStateAction<ArgBuilderItem[]>>;
  builderMsgs: MsgBuilderItem[];
  setBuilderMsgs: React.Dispatch<React.SetStateAction<MsgBuilderItem[]>>;
}

export const VisualPromptBuilder: React.FC<VisualPromptBuilderProps> = ({
  promptDesc,
  setPromptDesc,
  builderArgs,
  setBuilderArgs,
  builderMsgs,
  setBuilderMsgs,
}) => {
  const addArgument = () => {
    setBuilderArgs([...builderArgs, { id: Math.random().toString(), name: '', description: '', required: false }]);
  };

  const removeArgument = (id: string) => {
    setBuilderArgs(builderArgs.filter((a) => a.id !== id));
  };

  const updateArgument = (id: string, field: keyof ArgBuilderItem, val: any) => {
    setBuilderArgs(builderArgs.map((a) => (a.id === id ? { ...a, [field]: val } : a)));
  };

  const addMessage = (role: 'user' | 'assistant') => {
    setBuilderMsgs([...builderMsgs, { id: Math.random().toString(), role, text: '' }]);
  };

  const removeMessage = (id: string) => {
    setBuilderMsgs(builderMsgs.filter((m) => m.id !== id));
  };

  const updateMessageText = (id: string, text: string) => {
    setBuilderMsgs(builderMsgs.map((m) => (m.id === id ? { ...m, text } : m)));
  };

  return (
    <div className="visual-builder-container" style={{ maxHeight: '420px', overflowY: 'auto', paddingRight: '5px' }}>
      <div className="form-group">
        <label>Prompt Description</label>
        <input
          type="text"
          placeholder="Short description of what this prompt template does"
          value={promptDesc}
          onChange={(e) => setPromptDesc(e.target.value)}
        />
      </div>

      <div style={{ marginTop: '15px', marginBottom: '10px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h4 style={{ margin: 0, fontSize: '13px', color: 'var(--text-muted)' }}>
          <i className="fa-solid fa-sliders"></i> Prompt Arguments / Variables
        </h4>
        <button type="button" className="btn btn-secondary btn-sm" onClick={addArgument}>
          <i className="fa-solid fa-plus"></i> Add Variable
        </button>
      </div>

      {builderArgs.length === 0 ? (
        <div style={{ padding: '10px', background: 'rgba(255,255,255,0.02)', borderRadius: '6px', fontSize: '12px', color: 'var(--text-muted)' }}>
          No arguments defined.
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
          {builderArgs.map((arg) => (
            <div
              key={arg.id}
              style={{
                display: 'grid',
                gridTemplateColumns: '2fr 3fr auto auto',
                gap: '10px',
                alignItems: 'center',
                background: 'rgba(255,255,255,0.03)',
                padding: '8px',
                borderRadius: '6px',
              }}
            >
              <input
                type="text"
                placeholder="Variable Name (e.g. topic)"
                value={arg.name}
                onChange={(e) => updateArgument(arg.id, 'name', e.target.value)}
                style={{ fontSize: '12px' }}
              />
              <input
                type="text"
                placeholder="Description"
                value={arg.description}
                onChange={(e) => updateArgument(arg.id, 'description', e.target.value)}
                style={{ fontSize: '12px' }}
              />
              <label style={{ display: 'flex', alignItems: 'center', gap: '5px', fontSize: '11px', cursor: 'pointer' }}>
                <input
                  type="checkbox"
                  checked={arg.required}
                  onChange={(e) => updateArgument(arg.id, 'required', e.target.checked)}
                />
                Required
              </label>
              <button
                type="button"
                className="btn-icon"
                onClick={() => removeArgument(arg.id)}
                style={{ color: '#ef4444' }}
              >
                <i className="fa-solid fa-trash"></i>
              </button>
            </div>
          ))}
        </div>
      )}

      <div style={{ marginTop: '20px', marginBottom: '10px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h4 style={{ margin: 0, fontSize: '13px', color: 'var(--text-muted)' }}>
          <i className="fa-solid fa-comments"></i> Messages Sequence
        </h4>
        <div style={{ display: 'flex', gap: '8px' }}>
          <button type="button" className="btn btn-secondary btn-sm" onClick={() => addMessage('user')}>
            <i className="fa-solid fa-plus"></i> User Message
          </button>
          <button type="button" className="btn btn-secondary btn-sm" onClick={() => addMessage('assistant')}>
            <i className="fa-solid fa-plus"></i> Assistant Message
          </button>
        </div>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
        {builderMsgs.map((msg, index) => (
          <div
            key={msg.id}
            style={{
              background: 'rgba(255,255,255,0.03)',
              padding: '10px',
              borderRadius: '6px',
              borderLeft: `4px solid ${msg.role === 'user' ? 'var(--primary)' : 'var(--accent)'}`,
            }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '6px' }}>
              <span style={{ fontSize: '12px', fontWeight: 600, color: msg.role === 'user' ? 'var(--primary)' : 'var(--accent)' }}>
                #{index + 1} {msg.role.toUpperCase()}
              </span>
              <button
                type="button"
                className="btn-icon"
                onClick={() => removeMessage(msg.id)}
                style={{ color: '#ef4444' }}
              >
                <i className="fa-solid fa-trash"></i>
              </button>
            </div>
            <textarea
              rows={3}
              placeholder={`Enter ${msg.role} message. Use {{variable_name}} to inject arguments.`}
              value={msg.text}
              onChange={(e) => updateMessageText(msg.id, e.target.value)}
              style={{
                width: '100%',
                fontSize: '12px',
                fontFamily: 'monospace',
                background: 'var(--bg-dark)',
                color: 'var(--text-main)',
                border: '1px solid var(--border-color)',
                padding: '6px',
                borderRadius: '4px',
              }}
            />
          </div>
        ))}
      </div>
    </div>
  );
};
