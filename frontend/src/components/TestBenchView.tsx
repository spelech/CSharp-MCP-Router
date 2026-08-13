/* eslint-disable react-hooks/set-state-in-effect */
import React, { useEffect, useState } from 'react';
import { apiRequest } from '../utils/api';
import { showToast } from '../stores/useToastStore';

import { ToolTesterCard } from './testbench/ToolTesterCard';
import { PromptTesterCard } from './testbench/PromptTesterCard';
import { ResourceTesterCard } from './testbench/ResourceTesterCard';
import { SemanticRouterCard } from './testbench/SemanticRouterCard';
import { ConsoleCard } from './testbench/ConsoleCard';
import { LogsTerminalCard } from './testbench/LogsTerminalCard';

interface ToolItem {
  name: string;
  description: string;
  inputSchema?: {
    type?: string;
    properties?: Record<string, any>;
    required?: string[];
  };
}

interface PromptItem {
  name: string;
  description: string;
  arguments?: {
    name: string;
    description?: string;
    required?: boolean;
  }[];
}

interface ResourceItem {
  uri: string;
  name: string;
  mimeType?: string;
  description?: string;
}

interface TemplateItem {
  uriTemplate: string;
  name: string;
  description?: string;
}

export const TestBenchView: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'tools' | 'prompts' | 'resources'>('tools');

  // Lists
  const [tools, setTools] = useState<ToolItem[]>([]);
  const [prompts, setPrompts] = useState<PromptItem[]>([]);
  const [resourcesData, setResourcesData] = useState<{ resources: ResourceItem[]; templates: TemplateItem[] }>({ resources: [], templates: [] });

  // Tool Selects State
  const [selectedToolServer, setSelectedToolServer] = useState('');
  const [selectedToolName, setSelectedToolName] = useState('');
  const [toolArguments, setToolArguments] = useState<Record<string, any>>({});
  const [rawToolJson, setRawToolJson] = useState('{}');

  // Prompt Selects State
  const [selectedPromptServer, setSelectedPromptServer] = useState('');
  const [selectedPromptName, setSelectedPromptName] = useState('');
  const [promptArguments, setPromptArguments] = useState<Record<string, string>>({});

  // Resource Selects State
  const [selectedResourceServer, setSelectedResourceServer] = useState('');
  const [selectedResourceUri, setSelectedResourceUri] = useState('');
  const [selectedResourceValue, setSelectedResourceValue] = useState('');

  // Semantic
  const [semanticQuery, setSemanticQuery] = useState('');
  const [semanticResults, setSemanticResults] = useState<any[]>([]);
  const [isSearchingSemantic, setIsSearchingSemantic] = useState(false);

  // Console
  const [consoleRequest, setConsoleRequest] = useState('Ready');
  const [consoleResponse, setConsoleResponse] = useState('Waiting for execution...');

  const loadTools = async () => {
    try {
      const data = await apiRequest<ToolItem[]>('/api/test/tools');
      setTools(data || []);
    } catch (e) {
      console.error('Failed to load test tools:', e);
    }
  };

  const loadPrompts = async () => {
    try {
      const data = await apiRequest<PromptItem[]>('/api/test/prompts');
      setPrompts(data || []);
    } catch (e) {
      console.error('Failed to load prompts:', e);
    }
  };

  const loadResources = async () => {
    try {
      const data = await apiRequest<any>('/api/test/resources');
      setResourcesData(data || { resources: [], templates: [] });
    } catch (e) {
      console.error('Failed to load resources:', e);
    }
  };

  useEffect(() => {
    loadTools();
    loadPrompts();
    loadResources();
  }, []);

  // Run Tools Call
  const handleToolServerChange = (server: string) => {
    setSelectedToolServer(server);
    setSelectedToolName('');
    setToolArguments({});
    setRawToolJson('{}');
  };

  const handleToolNameChange = (name: string) => {
    setSelectedToolName(name);
    setToolArguments({});
    const tool = tools.find((t) => t.name === name);
    if (tool && tool.inputSchema && tool.inputSchema.properties) {
      const initial: Record<string, any> = {};
      Object.entries(tool.inputSchema.properties).forEach(([key, prop]: [string, any]) => {
        if (prop.type === 'boolean') {
          initial[key] = false;
        }
      });
      setToolArguments(initial);
      setRawToolJson(JSON.stringify(initial, null, 2));
    } else {
      setRawToolJson('{}');
    }
  };

  const handleArgInputChange = (key: string, type: string, value: any) => {
    let finalValue = value;
    if (type === 'boolean') {
      finalValue = !!value;
    } else if (type === 'number') {
      finalValue = value === '' ? '' : Number(value);
    } else if (type === 'array' || type === 'object') {
      try {
        finalValue = JSON.parse(value);
      } catch {
        finalValue = value;
      }
    }

    const updated = { ...toolArguments, [key]: finalValue };
    setToolArguments(updated);
    setRawToolJson(JSON.stringify(updated, null, 2));
  };

  const runToolCall = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedToolServer || !selectedToolName) return;

    setConsoleRequest('Formatting request...');
    setConsoleResponse('Executing...');

    const cleanName = selectedToolName.includes('__') ? selectedToolName.split('__')[1] : selectedToolName;

    const requestBlock = {
      jsonrpc: '2.0',
      id: 'client-call-id',
      method: 'tools/call',
      params: {
        name: selectedToolName,
        arguments: toolArguments,
      },
    };
    setConsoleRequest(JSON.stringify(requestBlock, null, 2));

    try {
      showToast(`Executing tool '${cleanName}'...`, 'info', 2500);
      const result = await apiRequest('/api/test/call', {
        method: 'POST',
        body: {
          serverId: selectedToolServer,
          toolName: cleanName,
          arguments: toolArguments,
        },
      });

      setConsoleResponse(JSON.stringify(result, null, 2));
      const errObj = result && (result.error || result.Error);
      if (errObj) {
        showToast(`Tool returned error: ${errObj.message || errObj.Message || 'Unknown error'}`, 'error');
      } else {
        showToast(`Tool '${cleanName}' executed successfully!`, 'success');
      }
    } catch (err: any) {
      setConsoleResponse(`Call failed:\n${err.message}`);
      showToast(`Tool execution failed: ${err.message}`, 'error');
    }
  };

  // Run Prompt
  const handlePromptServerChange = (server: string) => {
    setSelectedPromptServer(server);
    setSelectedPromptName('');
    setPromptArguments({});
  };

  const handlePromptNameChange = (name: string) => {
    setSelectedPromptName(name);
    setPromptArguments({});
  };

  const handlePromptArgChange = (name: string, val: string) => {
    setPromptArguments({ ...promptArguments, [name]: val });
  };

  const runPromptGet = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedPromptServer || !selectedPromptName) return;

    setConsoleRequest('Formatting request...');
    setConsoleResponse('Executing...');

    const requestBlock = {
      jsonrpc: '2.0',
      id: 'client-call-id',
      method: 'prompts/get',
      params: {
        name: selectedPromptName,
        arguments: promptArguments,
      },
    };
    setConsoleRequest(JSON.stringify(requestBlock, null, 2));

    const cleanName = selectedPromptName.includes('__') ? selectedPromptName.split('__')[1] : selectedPromptName;

    try {
      showToast(`Retrieving prompt '${cleanName}'...`, 'info', 2500);
      const result = await apiRequest('/api/test/prompts/get', {
        method: 'POST',
        body: {
          serverId: selectedPromptServer,
          promptName: cleanName,
          arguments: promptArguments,
        },
      });

      setConsoleResponse(JSON.stringify(result, null, 2));
      const errObj = result && (result.error || result.Error);
      if (errObj) {
        showToast(`Prompt error: ${errObj.message || errObj.Message || 'Unknown error'}`, 'error');
      } else {
        showToast(`Prompt '${cleanName}' retrieved successfully!`, 'success');
      }
    } catch (err: any) {
      setConsoleResponse(`Prompt execution failed:\n${err.message}`);
      showToast(`Prompt retrieval failed: ${err.message}`, 'error');
    }
  };

  // Run Resource Read
  const handleResourceServerChange = (server: string) => {
    setSelectedResourceServer(server);
    setSelectedResourceValue('');
    setSelectedResourceUri('');
  };

  const handleResourceSelectChange = (val: string, type: string) => {
    setSelectedResourceValue(val);
    if (type === 'template') {
      setSelectedResourceUri(val.replace('{server_name}', 'mcp-router'));
    } else {
      setSelectedResourceUri(val);
    }
  };

  const runResourceRead = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedResourceUri) return;

    setConsoleRequest('Formatting request...');
    setConsoleResponse('Executing...');

    const requestBlock = {
      jsonrpc: '2.0',
      id: 'client-call-id',
      method: 'resources/read',
      params: {
        uri: selectedResourceUri,
      },
    };
    setConsoleRequest(JSON.stringify(requestBlock, null, 2));

    try {
      showToast('Reading resource...', 'info', 2500);
      const result = await apiRequest('/api/test/resources/read', {
        method: 'POST',
        body: { uri: selectedResourceUri },
      });

      setConsoleResponse(JSON.stringify(result, null, 2));
      const errObj = result && (result.error || result.Error);
      if (errObj) {
        showToast(`Resource error: ${errObj.message || errObj.Message || 'Unknown error'}`, 'error');
      } else {
        showToast('Resource read successfully!', 'success');
      }
    } catch (err: any) {
      setConsoleResponse(`Resource read failed:\n${err.message}`);
      showToast(`Resource read failed: ${err.message}`, 'error');
    }
  };

  // Semantic
  const runSemanticSearch = async () => {
    if (!semanticQuery.trim()) return;
    setIsSearchingSemantic(true);
    setSemanticResults([]);

    try {
      const data = await apiRequest('/api/test/semantic-search', {
        method: 'POST',
        body: { query: semanticQuery },
      });
      setSemanticResults(data || []);
    } catch (err: any) {
      showToast(`Semantic Search Error: ${err.message}`, 'error');
    } finally {
      setIsSearchingSemantic(false);
    }
  };

  return (
    <div id="view-testbench" className="view-panel active">
      <div className="tester-tabs" style={{ justifyContent: 'flex-start', gap: '15px', marginBottom: '20px', borderBottom: '1px solid var(--border-color)', paddingBottom: '10px', width: '100%' }}>
        <button
          type="button"
          className={`tester-tab-btn tb-nav-btn ${activeTab === 'tools' ? 'active' : ''}`}
          onClick={() => setActiveTab('tools')}
        >
          <i className="fa-solid fa-screwdriver-wrench"></i> Tools
        </button>
        <button
          type="button"
          className={`tester-tab-btn tb-nav-btn ${activeTab === 'prompts' ? 'active' : ''}`}
          onClick={() => setActiveTab('prompts')}
        >
          <i className="fa-solid fa-comments"></i> Prompts
        </button>
        <button
          type="button"
          className={`tester-tab-btn tb-nav-btn ${activeTab === 'resources' ? 'active' : ''}`}
          onClick={() => setActiveTab('resources')}
        >
          <i className="fa-solid fa-file-invoice"></i> Resources
        </button>
      </div>

      <div className="tester-container">
        {/* Left Column: Form execution */}
        <div className="tester-panel">
          {activeTab === 'tools' && (
            <ToolTesterCard
              tools={tools}
              selectedServer={selectedToolServer}
              selectedToolName={selectedToolName}
              toolArguments={toolArguments}
              rawToolJson={rawToolJson}
              onServerChange={handleToolServerChange}
              onToolChange={handleToolNameChange}
              onArgChange={handleArgInputChange}
              onRawJsonChange={setRawToolJson}
              onSubmit={runToolCall}
            />
          )}

          {activeTab === 'prompts' && (
            <PromptTesterCard
              prompts={prompts}
              selectedServer={selectedPromptServer}
              selectedPromptName={selectedPromptName}
              promptArguments={promptArguments}
              onServerChange={handlePromptServerChange}
              onPromptChange={handlePromptNameChange}
              onArgChange={handlePromptArgChange}
              onSubmit={runPromptGet}
            />
          )}

          {activeTab === 'resources' && (
            <ResourceTesterCard
              resourcesData={resourcesData}
              selectedServer={selectedResourceServer}
              selectedResourceUri={selectedResourceUri}
              selectedResourceValue={selectedResourceValue}
              onServerChange={handleResourceServerChange}
              onSelectChange={handleResourceSelectChange}
              onUriChange={setSelectedResourceUri}
              onSubmit={runResourceRead}
            />
          )}
        </div>

        {/* Right Column: Console & Semantic */}
        <div className="tester-panel">
          <SemanticRouterCard
            semanticQuery={semanticQuery}
            semanticResults={semanticResults}
            isSearchingSemantic={isSearchingSemantic}
            onQueryChange={setSemanticQuery}
            onSearch={runSemanticSearch}
          />

          <ConsoleCard consoleRequest={consoleRequest} consoleResponse={consoleResponse} />
        </div>

        {/* Bottom Full Width System logs */}
        <LogsTerminalCard />
      </div>
    </div>
  );
};
export default TestBenchView;
