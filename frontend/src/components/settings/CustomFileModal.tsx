import React, { useState } from 'react';
import { useSettingsStore } from '../../stores/useSettingsStore';
import { showToast } from '../../stores/useToastStore';

interface ArgBuilderItem {
  id: string;
  name: string;
  description: string;
  required: boolean;
}

interface MsgBuilderItem {
  id: string;
  role: 'user' | 'assistant';
  text: string;
}

const initializePromptBuilder = (meta: { type: 'prompts' | 'resources'; name: string } | null, content: string) => {
  if (meta) {
    if (meta.type === 'prompts') {
      try {
        const parsed = JSON.parse(content);
        const desc = parsed.description || '';
        const args: ArgBuilderItem[] = Array.isArray(parsed.arguments)
          ? parsed.arguments.map((arg: { name?: string; description?: string; required?: boolean }) => ({
              id: Math.random().toString(),
              name: arg.name || '',
              description: arg.description || '',
              required: !!arg.required,
            }))
          : [];
        const msgs: MsgBuilderItem[] = Array.isArray(parsed.messages)
          ? parsed.messages.map((msg: { role?: string; content?: string | { text?: string } }) => {
              const text = msg.content && typeof msg.content === 'object' ? msg.content.text || '' : (msg.content || '');
              return {
                id: Math.random().toString(),
                role: msg.role === 'assistant' ? 'assistant' : 'user',
                text,
              };
            })
          : [];
        return { promptDesc: desc, builderArgs: args, builderMsgs: msgs };
      } catch {
        return { promptDesc: '', builderArgs: [], builderMsgs: [] };
      }
    }
    return { promptDesc: '', builderArgs: [], builderMsgs: [] };
  }
  return {
    promptDesc: 'My custom prompt description',
    builderArgs: [{ id: '1', name: 'topic', description: 'Topic to write about', required: true }],
    builderMsgs: [
      {
        id: '1',
        role: 'user' as const,
        text: 'Write a short summary about {{topic}}.',
      },
    ],
  };
};

const CustomFileModalDialog: React.FC = () => {
  const {
    editingFileMeta,
    editingFileContent,
    activeFileModalTab,
    saveCustomFile,
    closeCustomFileModal,
    setActiveFileModalTab,
  } = useSettingsStore();

  const [fileType, setFileType] = useState<'prompts' | 'resources'>(editingFileMeta?.type || 'prompts');
  const [fileName, setFileName] = useState(editingFileMeta?.name || '');
  const [rawContent, setRawContent] = useState(editingFileContent);

  const initialPromptData = initializePromptBuilder(editingFileMeta, editingFileContent);
  const [promptDesc, setPromptDesc] = useState(initialPromptData.promptDesc);
  const [builderArgs, setBuilderArgs] = useState<ArgBuilderItem[]>(initialPromptData.builderArgs);
  const [builderMsgs, setBuilderMsgs] = useState<MsgBuilderItem[]>(initialPromptData.builderMsgs);

  const compileBuilderToJson = (): string => {
    const promptObj = {
      description: promptDesc,
      arguments: builderArgs
        .filter((arg) => arg.name.trim() !== '')
        .map((arg) => ({
          name: arg.name.trim(),
          description: arg.description.trim(),
          required: arg.required,
        })),
      messages: builderMsgs.map((msg) => ({
        role: msg.role,
        content: {
          type: 'text',
          text: msg.text,
        },
      })),
    };
    return JSON.stringify(promptObj, null, 2);
  };

  const handleTabSwitch = (tab: 'editor' | 'builder') => {
    if (tab === 'editor' && activeFileModalTab === 'builder' && fileType === 'prompts') {
      // Sync builder -> raw JSON
      setRawContent(compileBuilderToJson());
    } else if (tab === 'builder' && activeFileModalTab === 'editor' && fileType === 'prompts') {
      // Sync raw JSON -> builder
      try {
        const parsed = JSON.parse(rawContent);
        setPromptDesc(parsed.description || '');
        if (Array.isArray(parsed.arguments)) {
          setBuilderArgs(
            parsed.arguments.map((arg: any) => ({
              id: Math.random().toString(),
              name: arg.name || '',
              description: arg.description || '',
              required: !!arg.required,
            }))
          );
        }
        if (Array.isArray(parsed.messages)) {
          setBuilderMsgs(
            parsed.messages.map((msg: any) => ({
              id: Math.random().toString(),
              role: msg.role === 'assistant' ? 'assistant' : 'user',
              text: msg.content && typeof msg.content === 'object' ? msg.content.text || '' : (msg.content || ''),
            }))
          );
        }
      } catch {
        showToast('Cannot switch to Visual Builder: JSON in editor is invalid.', 'error');
        return;
      }
    }
    setActiveFileModalTab(tab);
  };

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

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!fileName.trim()) {
      showToast('Please enter a file name.', 'error');
      return;
    }

    let finalContent = rawContent;
    if (fileType === 'prompts' && activeFileModalTab === 'builder') {
      finalContent = compileBuilderToJson();
    }

    // Ensure valid JSON if prompt
    if (fileType === 'prompts') {
      try {
        JSON.parse(finalContent);
      } catch {
        showToast('Invalid JSON content. Please check syntax or use the Visual Builder.', 'error');
        return;
      }
    }

    const success = await saveCustomFile(fileType, fileName.trim(), finalContent);
    if (success) {
      closeCustomFileModal();
    }
  };

  return (
    <div className="modal-backdrop" id="custom-file-modal" style={{ display: 'flex' }}>
      <div className="glass-card modal-card" style={{ maxWidth: '850px', width: '90%' }}>
        <div className="modal-header">
          <h2>
            <i className="fa-solid fa-file-code"></i> {editingFileMeta ? `Edit ${editingFileMeta.name}` : 'Create Custom File'}
          </h2>
          <button type="button" className="btn-close" onClick={closeCustomFileModal}>
            &times;
          </button>
        </div>

        <form id="custom-file-form" onSubmit={handleSave}>
          <div className="form-row" style={{ display: 'grid', gridTemplateColumns: '1fr 2fr', gap: '15px' }}>
            <div className="form-group">
              <label htmlFor="custom-file-type">File Type</label>
              <select
                id="custom-file-type"
                value={fileType}
                disabled={!!editingFileMeta}
                onChange={(e) => {
                  const newType = e.target.value as 'prompts' | 'resources';
                  setFileType(newType);
                  if (newType === 'prompts' && !fileName.endsWith('.json')) {
                    setFileName((prev) => prev.replace(/\.[^/.]+$/, '') + '.json');
                  } else if (newType === 'resources' && !fileName.endsWith('.md') && !fileName.endsWith('.txt')) {
                    setFileName((prev) => prev.replace(/\.[^/.]+$/, '') + '.md');
                  }
                }}
              >
                <option value="prompts">Prompt (JSON)</option>
                <option value="resources">Resource (Markdown/Text)</option>
              </select>
            </div>

            <div className="form-group">
              <label htmlFor="custom-file-name">File Name</label>
              <input
                type="text"
                id="custom-file-name"
                placeholder={fileType === 'prompts' ? 'e.g. system-summary.json' : 'e.g. guide.md'}
                value={fileName}
                disabled={!!editingFileMeta}
                onChange={(e) => setFileName(e.target.value)}
                required
              />
            </div>
          </div>

          {fileType === 'prompts' && (
            <div className="tester-tabs" style={{ marginBottom: '15px', marginTop: '5px' }}>
              <button
                type="button"
                className={`tester-tab-btn ${activeFileModalTab === 'editor' ? 'active' : ''}`}
                onClick={() => handleTabSwitch('editor')}
              >
                <i className="fa-solid fa-code"></i> Raw JSON Editor
              </button>
              <button
                type="button"
                className={`tester-tab-btn ${activeFileModalTab === 'builder' ? 'active' : ''}`}
                onClick={() => handleTabSwitch('builder')}
              >
                <i className="fa-solid fa-wand-magic-sparkles"></i> Visual Prompt Builder
              </button>
            </div>
          )}

          {activeFileModalTab === 'editor' || fileType === 'resources' ? (
            <div className="form-group">
              <label htmlFor="custom-file-content">File Content</label>
              <textarea
                id="custom-file-content"
                rows={14}
                style={{
                  fontFamily: 'JetBrains Mono, monospace',
                  fontSize: '13px',
                  background: 'var(--bg-dark)',
                  color: 'var(--text-main)',
                  border: '1px solid var(--border-color)',
                  width: '100%',
                  padding: '10px',
                  borderRadius: '6px',
                }}
                value={rawContent}
                onChange={(e) => setRawContent(e.target.value)}
                required
              />
            </div>
          ) : (
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
          )}

          <div className="modal-footer" style={{ marginTop: '15px' }}>
            <button type="button" className="btn btn-secondary" onClick={closeCustomFileModal}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary" id="btn-save-custom-file">
              Save File
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export const CustomFileModal: React.FC = () => {
  const { isCustomFileOpen, editingFileMeta } = useSettingsStore();

  if (!isCustomFileOpen) return null;

  return <CustomFileModalDialog key={editingFileMeta ? `${editingFileMeta.type}-${editingFileMeta.name}` : 'new'} />;
};
