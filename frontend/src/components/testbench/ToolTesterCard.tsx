import React, { useState } from 'react';
import { ToolItem } from '../../shared/types/testbench';
import { extractPropertiesFromSchema } from '../../utils/schemaUtils';

interface ToolTesterCardProps {
  tools: ToolItem[];
  selectedServer: string;
  selectedToolName: string;
  toolArguments: Record<string, any>;
  rawToolJson: string;
  onServerChange: (srv: string) => void;
  onToolChange: (name: string) => void;
  onArgChange: (key: string, type: string, val: any) => void;
  onRawJsonChange: (val: string) => void;
  onSubmit: (e: React.FormEvent) => void;
}

export const ToolTesterCard: React.FC<ToolTesterCardProps> = ({
  tools,
  selectedServer,
  selectedToolName,
  toolArguments,
  rawToolJson,
  onServerChange,
  onToolChange,
  onArgChange,
  onRawJsonChange,
  onSubmit,
}) => {
  const [interactiveTab, setInteractiveTab] = useState<'form' | 'json'>('form');

  const getToolServers = () => {
    const servers = new Set<string>();
    servers.add('custom');
    tools.forEach((t) => {
      const parts = t.name.split('__');
      if (parts.length > 1) {
        servers.add(parts[0]);
      } else {
        servers.add('custom');
      }
    });
    return Array.from(servers).sort();
  };

  const getFilteredTools = () => {
    return tools
      .filter((t) => {
        if (selectedServer === 'custom') {
          return !t.name.includes('__');
        }
        return t.name.startsWith(selectedServer + '__');
      })
      .sort((a, b) => a.name.localeCompare(b.name));
  };

  const currentTool = tools.find((t) => t.name === selectedToolName);

  return (
    <div className="glass-card">
      <h2>
        <i className="fa-solid fa-wand-magic-sparkles"></i> Interactive Tool Tester
      </h2>
      <form onSubmit={onSubmit}>
        <div className="form-row">
          <div className="form-group">
            <label htmlFor="tester-server">Server</label>
            <select
              id="tester-server"
              value={selectedServer}
              onChange={(e) => onServerChange(e.target.value)}
              required
            >
              <option value="">-- Choose Server --</option>
              {getToolServers().map((srv) => (
                <option key={srv} value={srv}>
                  {srv === 'custom' ? 'Native C# Registry (custom)' : `${srv.toUpperCase()} Server`}
                </option>
              ))}
            </select>
          </div>
          <div className="form-group">
            <label htmlFor="tester-tool">Tool</label>
            <select
              id="tester-tool"
              value={selectedToolName}
              onChange={(e) => onToolChange(e.target.value)}
              required
            >
              <option value="">-- Choose Tool --</option>
              {getFilteredTools().map((t) => {
                const display = t.name.includes('__') ? t.name.split('__')[1] : t.name;
                return (
                  <option key={t.name} value={t.name}>
                    {display}
                  </option>
                );
              })}
            </select>
          </div>
        </div>

        <div className="tester-tabs">
          <button
            type="button"
            className={`tester-tab-btn ${interactiveTab === 'form' ? 'active' : ''}`}
            onClick={() => setInteractiveTab('form')}
          >
            Interactive Form
          </button>
          <button
            type="button"
            className={`tester-tab-btn ${interactiveTab === 'json' ? 'active' : ''}`}
            onClick={() => setInteractiveTab('json')}
          >
            Raw JSON Input
          </button>
        </div>

        {interactiveTab === 'form' ? (
          <div className="tester-tab-content active">
            <div id="dynamic-form-fields">
              {selectedToolName && currentTool ? (
                renderDynamicFields(currentTool, toolArguments, onArgChange, () => setInteractiveTab('json'))
              ) : (
                <div className="empty-state">Select a tool to generate parameters.</div>
              )}
            </div>
          </div>
        ) : (
          <div className="tester-tab-content active">
            <div className="form-group">
              <label htmlFor="tester-raw-json">Arguments (JSON)</label>
              <textarea
                id="tester-raw-json"
                rows={8}
                placeholder="{}"
                value={rawToolJson}
                onChange={(e) => onRawJsonChange(e.target.value)}
              ></textarea>
            </div>
          </div>
        )}

        <div style={{ marginTop: '20px' }}>
          <button type="submit" className="btn btn-primary" disabled={!selectedToolName}>
            <i className="fa-solid fa-play"></i> Run Tool
          </button>
        </div>
      </form>
    </div>
  );
};

const renderDynamicFields = (
  tool: ToolItem,
  args: Record<string, any>,
  onChange: (key: string, type: string, val: any) => void,
  switchToRawJson: () => void
) => {
  const { properties, required, hasSchemaKeywords } = extractPropertiesFromSchema(tool.inputSchema);
  const propertyEntries = Object.entries(properties);

  if (propertyEntries.length === 0) {
    if (tool.inputSchema && hasSchemaKeywords) {
      return (
        <div className="empty-state" style={{ textAlign: 'left', padding: '16px' }}>
          <p style={{ marginBottom: '12px' }}>
            <i className="fa-solid fa-circle-info" style={{ marginRight: '6px', color: 'var(--accent-color, #3b82f6)' }}></i>
            This tool uses a dynamic or complex JSON Schema (JSON Schema 2020-12 / anyOf / allOf / non-object schema).
          </p>
          <button
            type="button"
            className="btn btn-secondary"
            onClick={switchToRawJson}
            style={{ fontSize: '0.85rem' }}
          >
            Switch to Raw JSON Input
          </button>
        </div>
      );
    }
    return <div className="empty-state">This tool takes no arguments.</div>;
  }

  return propertyEntries.map(([key, prop]: [string, any]) => {
    const isRequired = required.includes(key);
    const reqText = isRequired ? <span style={{ color: 'var(--status-offline)' }}>*</span> : null;

    if (prop.type === 'boolean') {
      return (
        <div key={key} className="param-field checkbox-field">
          <label className="switch">
            <input
              type="checkbox"
              checked={!!args[key]}
              onChange={(e) => onChange(key, 'boolean', e.target.checked)}
            />
            <span className="slider"></span>
          </label>
          <label>
            {key} {reqText}
          </label>
          {prop.description && <div className="field-desc">{prop.description}</div>}
        </div>
      );
    }

    if (prop.type === 'integer' || prop.type === 'number') {
      return (
        <div key={key} className="param-field">
          <label>
            {key} {reqText}
          </label>
          <input
            type="number"
            step={prop.type === 'integer' ? '1' : 'any'}
            value={args[key] !== undefined ? args[key] : ''}
            onChange={(e) => onChange(key, 'number', e.target.value)}
            required={isRequired}
          />
          {prop.description && <div className="field-desc">{prop.description}</div>}
        </div>
      );
    }

    if (prop.type === 'array' || prop.type === 'object') {
      const displayVal = typeof args[key] === 'object' ? JSON.stringify(args[key], null, 2) : (args[key] || '');
      return (
        <div key={key} className="param-field">
          <label>
            {key} {reqText}
          </label>
          <textarea
            rows={2}
            placeholder={prop.type === 'array' ? '["item1", "item2"]' : '{"key": "value"}'}
            value={displayVal}
            onChange={(e) => onChange(key, prop.type, e.target.value)}
            required={isRequired}
          />
          {prop.description && <div className="field-desc">{prop.description}</div>}
        </div>
      );
    }

    return (
      <div key={key} className="param-field">
        <label>
          {key} {reqText}
        </label>
        <input
          type="text"
          value={args[key] || ''}
          onChange={(e) => onChange(key, 'string', e.target.value)}
          required={isRequired}
        />
        {prop.description && <div className="field-desc">{prop.description}</div>}
      </div>
    );
  });
};
