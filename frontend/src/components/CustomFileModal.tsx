import React, { useEffect, useState } from 'react';
import { useSettingsStore } from '../stores/useSettingsStore';

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

export const CustomFileModal: React.FC = () => {
  const {
    isCustomFileOpen,
    editingFileMeta,
    editingFileContent,
    activeFileModalTab,
    saveCustomFile,
    closeCustomFileModal,
    setActiveFileModalTab,
  } = useSettingsStore();

  const [fileType, setFileType] = useState<'prompts' | 'resources'>('prompts');
  const [fileName, setFileName] = useState('');
  const [rawContent, setRawContent] = useState('');

  // Builder States
  const [promptDesc, setPromptDesc] = useState('');
  const [builderArgs, setBuilderArgs] = useState<ArgBuilderItem[]>([]);
  const [builderMsgs, setBuilderMsgs] = useState<MsgBuilderItem[]>([]);

  useEffect(() => {
    if (isCustomFileOpen) {
      if (editingFileMeta) {
        setFileType(editingFileMeta.type);
        setFileName(editingFileMeta.name);
        setRawContent(editingFileContent);

        // Try parsing to initialize prompt builder
        if (editingFileMeta.type === 'prompts') {
          try {
            const parsed = JSON.parse(editingFileContent);
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
            } else {
              setBuilderArgs([]);
            }

            if (Array.isArray(parsed.messages)) {
              setBuilderMsgs(
                parsed.messages.map((msg: any) => {
                  const text = msg.content && typeof msg.content === 'object' ? (msg.content.text || '') : (msg.content || '');
                  return {
                    id: Math.random().toString(),
                    role: msg.role === 'assistant' ? 'assistant' : 'user',
                    text,
                  };
                })
              );
            } else {
              setBuilderMsgs([]);
            }
          } catch {
            setPromptDesc('');
            setBuilderArgs([]);
            setBuilderMsgs([]);
          }
        }
      } else {
        setFileType('prompts');
        setFileName('');
        setRawContent(editingFileContent);
        setPromptDesc('My custom prompt description');
        setBuilderArgs([{ id: '1', name: 'topic', description: 'Topic to write about', required: true }]);
        setBuilderMsgs([
          {
            id: '1',
            role: 'user',
            text: 'Write a short summary about {{topic}}.',
          },
        ]);
      }
    }
  }, [isCustomFileOpen, editingFileMeta, editingFileContent]);

  if (!isCustomFileOpen) return null;

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
    if (tab === 'builder') {
      try {
        const parsed = JSON.parse(rawContent);
        setPromptDesc(parsed.description || '');
        setBuilderArgs(
          (parsed.arguments || []).map((arg: any) => ({
            id: Math.random().toString(),
            name: arg.name || '',
            description: arg.description || '',
            required: !!arg.required,
          }))
        );
        setBuilderMsgs(
          (parsed.messages || []).map((msg: any) => {
            const text = msg.content && typeof msg.content === 'object' ? (msg.content.text || '') : (msg.content || '');
            return {
              id: Math.random().toString(),
              role: msg.role === 'assistant' ? 'assistant' : 'user',
              text,
            };
          })
        );
        setActiveFileModalTab('builder');
      } catch (err: any) {
        alert(`Invalid JSON format in raw code editor. Please fix syntax errors before switching to Prompt Builder. Details: ${err.message}`);
      }
    } else {
      // compile builder values back into raw json content
      if (activeFileModalTab === 'builder') {
        setRawContent(compileBuilderToJson());
      }
      setActiveFileModalTab('editor');
    }
  };

  const handleAddArg = () => {
    setBuilderArgs([...builderArgs, { id: Math.random().toString(), name: '', description: '', required: false }]);
  };

  const handleRemoveArg = (id: string) => {
    setBuilderArgs(builderArgs.filter((arg) => arg.id !== id));
  };

  const handleArgChange = (id: string, field: keyof ArgBuilderItem, val: any) => {
    setBuilderArgs(
      builderArgs.map((arg) => (arg.id === id ? { ...arg, [field]: val } : arg))
    );
  };

  const handleAddMsg = () => {
    setBuilderMsgs([...builderMsgs, { id: Math.random().toString(), role: 'user', text: '' }]);
  };

  const handleRemoveMsg = (id: string) => {
    setBuilderMsgs(builderMsgs.filter((msg) => msg.id !== id));
  };

  const handleMsgChange = (id: string, field: keyof MsgBuilderItem, val: any) => {
    setBuilderMsgs(
      builderMsgs.map((msg) => (msg.id === id ? { ...msg, [field]: val } : msg))
    );
  };

  const handleTypeChange = (type: 'prompts' | 'resources') => {
    setFileType(type);
    if (type === 'prompts') {
      const defaultJson = JSON.stringify({
        description: "My custom prompt description",
        arguments: [
          { name: "topic", description: "Topic to write about", required: true }
        ],
        messages: [
          {
            role: "user",
            content: {
              type: "text",
              text: "Write a short summary about {{topic}}."
            }
          }
        ]
      }, null, 2);
      setRawContent(defaultJson);
    } else {
      setRawContent("# Local Resource File\nEnter markdown content here.");
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    let finalContent = rawContent;
    if (fileType === 'prompts' && activeFileModalTab === 'builder') {
      finalContent = compileBuilderToJson();
    }

    if (fileType === 'prompts') {
      try {
        JSON.parse(finalContent);
      } catch (err: any) {
        alert(`Invalid JSON format: ${err.message}`);
        return;
      }
    }

    let targetName = fileName.trim();
    if (!targetName) return;

    if (fileType === 'prompts' && !targetName.endsWith('.json')) {
      targetName += '.json';
    }

    const saved = await saveCustomFile(fileType, targetName, finalContent);
    if (saved) {
      closeCustomFileModal();
    }
  };

  const showTabsBar = fileType === 'prompts';

  return (
    <div className="modal-backdrop" id="custom-file-modal" style={{ display: 'flex' }}>
      <div className="glass-card modal-card" style={{ maxWidth: '700px', width: '90%' }}>
        <div className="modal-header">
          <h2 id="custom-file-modal-title">
            <i className="fa-solid fa-file-pen"></i> {editingFileMeta ? 'Edit Custom File' : 'Create Custom File'}
          </h2>
          <button type="button" className="btn-close" onClick={closeCustomFileModal}>
            &times;
          </button>
        </div>
        <form id="custom-file-form" onSubmit={handleSubmit}>
          <div className="form-row">
            <div className="form-group">
              <label htmlFor="custom-file-type">File Type</label>
              <select
                id="custom-file-type"
                value={fileType}
                onChange={(e) => handleTypeChange(e.target.value as any)}
                disabled={!!editingFileMeta}
                required
              >
                <option value="prompts">Prompt Template (JSON)</option>
                <option value="resources">Resource (Markdown/Text)</option>
              </select>
            </div>
            <div className="form-group">
              <label htmlFor="custom-file-name">Filename</label>
              <input
                type="text"
                id="custom-file-name"
                placeholder={fileType === 'prompts' ? 'e.g. my-custom-prompt.json' : 'e.g. todo.md'}
                value={fileName}
                onChange={(e) => setFileName(e.target.value)}
                disabled={!!editingFileMeta}
                required
              />
            </div>
          </div>

          {showTabsBar && (
            <div className="tester-tabs" id="custom-file-tabs-bar" style={{ marginBottom: '15px' }}>
              <button
                type="button"
                className={`tester-tab-btn ${activeFileModalTab === 'editor' ? 'active' : ''}`}
                onClick={() => handleTabSwitch('editor')}
                style={{ padding: '6px 12px', fontSize: '13px' }}
              >
                <i className="fa-solid fa-code"></i> Raw Code Editor
              </button>
              <button
                type="button"
                className={`tester-tab-btn ${activeFileModalTab === 'builder' ? 'active' : ''}`}
                onClick={() => handleTabSwitch('builder')}
                style={{ padding: '6px 12px', fontSize: '13px' }}
              >
                <i className="fa-solid fa-wand-magic-sparkles"></i> Prompt Builder
              </button>
            </div>
          )}

          {activeFileModalTab === 'editor' || !showTabsBar ? (
            <div id="custom-file-panel-editor">
              <div className="form-group">
                <label htmlFor="custom-file-content">File Content</label>
                <textarea
                  id="custom-file-content"
                  rows={15}
                  className="code-block"
                  style={{
                    width: '100%',
                    fontFamily: 'monospace',
                    background: 'rgba(0,0,0,0.3)',
                    border: '1px solid var(--border-color)',
                    color: '#fff',
                    padding: '10px',
                    borderRadius: '6px',
                    resize: 'vertical',
                  }}
                  placeholder="File contents..."
                  value={rawContent}
                  onChange={(e) => setRawContent(e.target.value)}
                />
                <small id="custom-file-editor-hint" style={{ color: 'var(--text-muted)', display: 'block', marginTop: '5px' }}>
                  {fileType === 'prompts'
                    ? 'JSON prompts must match the MCP Prompt template schema (containing "description", "arguments", "messages").'
                    : 'Markdown resources support rich-text layout definitions.'}
                </small>
              </div>
            </div>
          ) : (
            <div
              id="custom-file-panel-builder"
              style={{
                maxHeight: '400px',
                overflowY: 'auto',
                padding: '15px',
                border: '1px solid var(--border-color)',
                borderRadius: '6px',
                background: 'rgba(0,0,0,0.2)',
                marginBottom: '15px',
              }}
            >
              <div className="form-group">
                <label htmlFor="builder-prompt-desc">Prompt Description</label>
                <input
                  type="text"
                  id="builder-prompt-desc"
                  placeholder="e.g. Assist the user with codebase refactoring"
                  value={promptDesc}
                  onChange={(e) => setPromptDesc(e.target.value)}
                />
              </div>

              <div style={{ marginTop: '15px', borderTop: '1px solid var(--border-color)', paddingTop: '10px' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '10px' }}>
                  <h4 style={{ margin: 0, fontSize: '14px' }}>
                    <i className="fa-solid fa-list-ul"></i> Arguments
                  </h4>
                  <button
                    type="button"
                    className="btn btn-secondary btn-sm"
                    onClick={handleAddArg}
                    style={{ padding: '2px 8px', fontSize: '11px' }}
                  >
                    <i className="fa-solid fa-plus"></i> Add Arg
                  </button>
                </div>
                <div id="builder-args-list" style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                  {builderArgs.map((arg) => (
                    <div
                      key={arg.id}
                      className="form-row builder-arg-row"
                      style={{ alignItems: 'center', gap: '8px', marginBottom: '6px' }}
                    >
                      <input
                        type="text"
                        placeholder="Arg Name"
                        className="arg-name"
                        value={arg.name}
                        onChange={(e) => handleArgChange(arg.id, 'name', e.target.value)}
                        style={{ flex: 2, height: '32px', fontSize: '13px' }}
                        required
                      />
                      <input
                        type="text"
                        placeholder="Description"
                        className="arg-desc"
                        value={arg.description}
                        onChange={(e) => handleArgChange(arg.id, 'description', e.target.value)}
                        style={{ flex: 3, height: '32px', fontSize: '13px' }}
                      />
                      <label style={{ display: 'flex', alignItems: 'center', gap: '4px', fontSize: '12px', cursor: 'pointer', whiteSpace: 'nowrap', marginBottom: 0 }}>
                        <input
                          type="checkbox"
                          className="arg-req"
                          checked={arg.required}
                          onChange={(e) => handleArgChange(arg.id, 'required', e.target.checked)}
                        />{' '}
                        Req
                      </label>
                      <button
                        type="button"
                        className="btn btn-danger btn-sm btn-remove-row"
                        onClick={() => handleRemoveArg(arg.id)}
                        style={{ padding: '4px 8px', height: '32px' }}
                      >
                        <i className="fa-solid fa-trash"></i>
                      </button>
                    </div>
                  ))}
                </div>
              </div>

              <div style={{ marginTop: '15px', borderTop: '1px solid var(--border-color)', paddingTop: '10px' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '10px' }}>
                  <h4 style={{ margin: 0, fontSize: '14px' }}>
                    <i className="fa-solid fa-message"></i> Messages
                  </h4>
                  <button
                    type="button"
                    className="btn btn-secondary btn-sm"
                    onClick={handleAddMsg}
                    style={{ padding: '2px 8px', fontSize: '11px' }}
                  >
                    <i className="fa-solid fa-plus"></i> Add Message
                  </button>
                </div>
                <div id="builder-msgs-list" style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                  {builderMsgs.map((msg) => (
                    <div
                      key={msg.id}
                      className="builder-msg-row"
                      style={{
                        display: 'flex',
                        flexDirection: 'column',
                        gap: '6px',
                        border: '1px solid var(--border-color)',
                        padding: '8px',
                        borderRadius: '6px',
                        background: 'rgba(255,255,255,0.02)',
                        marginBottom: '8px',
                      }}
                    >
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <select
                          className="msg-role"
                          value={msg.role}
                          onChange={(e) => handleMsgChange(msg.id, 'role', e.target.value)}
                          style={{ width: '120px', height: '28px', fontSize: '12px', padding: '2px' }}
                        >
                          <option value="user">User</option>
                          <option value="assistant">Assistant</option>
                        </select>
                        <button
                          type="button"
                          className="btn btn-danger btn-sm btn-remove-row"
                          onClick={() => handleRemoveMsg(msg.id)}
                          style={{ padding: '2px 6px', fontSize: '11px' }}
                        >
                          <i className="fa-solid fa-trash"></i> Delete
                        </button>
                      </div>
                      <textarea
                        placeholder="Message content..."
                        className="msg-text"
                        rows={3}
                        style={{
                          width: '100%',
                          fontSize: '12px',
                          padding: '6px',
                          background: 'rgba(0,0,0,0.2)',
                          border: '1px solid var(--border-color)',
                          color: '#fff',
                          borderRadius: '4px',
                          resize: 'vertical',
                        }}
                        value={msg.text}
                        onChange={(e) => handleMsgChange(msg.id, 'text', e.target.value)}
                        required
                      />
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}

          <div className="modal-footer">
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
