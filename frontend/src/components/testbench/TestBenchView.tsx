import React, { useEffect, useState } from 'react';
import { apiRequest } from '../../shared/api/api';
import { showToast } from '../../stores/useToastStore';
import { ToolItem, PromptItem, ResourceItem, TemplateItem } from '../../shared/types';
import { fetchTestToolsApi, fetchTestPromptsApi, fetchTestResourcesApi } from '../../api/testbenchApi';

import { ToolTesterCard } from './ToolTesterCard';
import { PromptTesterCard } from './PromptTesterCard';
import { ResourceTesterCard } from './ResourceTesterCard';
import { SemanticRouterCard } from './SemanticRouterCard';
import { ConsoleCard } from './ConsoleCard';
import { LogsTerminalCard } from './LogsTerminalCard';

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

  useEffect(() => {
    let ignore = false;
    const fetchInitialData = async () => {
      try {
        const [toolsRes, promptsRes, resourcesRes] = await Promise.allSettled([
          fetchTestToolsApi(),
          fetchTestPromptsApi(),
          fetchTestResourcesApi(),
        ]);
        if (!ignore) {
          if (toolsRes.status === 'fulfilled') {
            setTools(toolsRes.value || []);
          }
          if (promptsRes.status === 'fulfilled') {
            setPrompts(promptsRes.value || []);
          }
          if (resourcesRes.status === 'fulfilled') {
            setResourcesData(resourcesRes.value || { resources: [], templates: [] });
          }
        }
      } catch (e) {
        console.error('Failed to fetch initial test data:', e);
      }
    };

    fetchInitialData();
    return () => {
      ignore = true;
    };
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
    if (!selectedToolName) {
      showToast('Please select a tool to execute', 'error');
      return;
    }

    let parsedArgs = toolArguments;
    try {
      if (rawToolJson && rawToolJson.trim() !== '') {
        parsedArgs = JSON.parse(rawToolJson);
      }
    } catch {
      showToast('Invalid raw arguments JSON', 'error');
      return;
    }

    const rpcPayload = {
      jsonrpc: '2.0',
      id: Math.floor(Math.random() * 1000000),
      method: 'tools/call',
      params: {
        name: selectedToolName,
        arguments: parsedArgs
      }
    };

    setConsoleRequest(JSON.stringify(rpcPayload, null, 2));
    setConsoleResponse('Executing tool...');

    try {
      const result = await apiRequest('/api/test/call-tool', {
        method: 'POST',
        body: {
          name: selectedToolName,
          arguments: parsedArgs
        }
      });
      setConsoleResponse(JSON.stringify(result, null, 2));
    } catch (err: any) {
      setConsoleResponse(`Error: ${err.message}`);
    }
  };

  // Prompts handlers
  const handlePromptServerChange = (server: string) => {
    setSelectedPromptServer(server);
    setSelectedPromptName('');
    setPromptArguments({});
  };

  const handlePromptNameChange = (name: string) => {
    setSelectedPromptName(name);
    setPromptArguments({});
  };

  const handlePromptArgChange = (name: string, value: string) => {
    setPromptArguments({ ...promptArguments, [name]: value });
  };

  const runPromptGet = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedPromptName) {
      showToast('Please select a prompt template', 'error');
      return;
    }

    const rpcPayload = {
      jsonrpc: '2.0',
      id: Math.floor(Math.random() * 1000000),
      method: 'prompts/get',
      params: {
        name: selectedPromptName,
        arguments: promptArguments
      }
    };

    setConsoleRequest(JSON.stringify(rpcPayload, null, 2));
    setConsoleResponse('Fetching prompt...');

    try {
      const result = await apiRequest('/api/test/get-prompt', {
        method: 'POST',
        body: {
          name: selectedPromptName,
          arguments: promptArguments
        }
      });
      setConsoleResponse(JSON.stringify(result, null, 2));
    } catch (err: any) {
      setConsoleResponse(`Error: ${err.message}`);
    }
  };

  // Resources handlers
  const handleResourceServerChange = (server: string) => {
    setSelectedResourceServer(server);
    setSelectedResourceUri('');
    setSelectedResourceValue('');
  };

  const handleResourceSelectChange = (val: string) => {
    setSelectedResourceValue(val);
    setSelectedResourceUri(val);
  };

  const runResourceRead = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedResourceUri) {
      showToast('Please enter or select a resource URI', 'error');
      return;
    }

    const rpcPayload = {
      jsonrpc: '2.0',
      id: Math.floor(Math.random() * 1000000),
      method: 'resources/read',
      params: {
        uri: selectedResourceUri
      }
    };

    setConsoleRequest(JSON.stringify(rpcPayload, null, 2));
    setConsoleResponse('Reading resource...');

    try {
      const result = await apiRequest('/api/test/read-resource', {
        method: 'POST',
        body: {
          uri: selectedResourceUri
        }
      });
      setConsoleResponse(JSON.stringify(result, null, 2));
    } catch (err: any) {
      setConsoleResponse(`Error: ${err.message}`);
    }
  };

  // Semantic
  const runSemanticSearch = async (e?: React.FormEvent) => {
    if (e && e.preventDefault) e.preventDefault();
    if (!semanticQuery.trim()) return;

    setIsSearchingSemantic(true);
    try {
      const results = await apiRequest<any[]>('/api/test/semantic-search', {
        method: 'POST',
        body: { query: semanticQuery.trim() }
      });
      setSemanticResults(results || []);
    } catch (err: any) {
      showToast(`Semantic search failed: ${err.message}`, 'error');
      setSemanticResults([]);
    } finally {
      setIsSearchingSemantic(false);
    }
  };

  return (
    <div id="view-testbench" className="view-panel active">
      {/* Top Tab Switcher */}
      <div className="tester-tabs">
        <button
          type="button"
          className={`tester-tab-btn ${activeTab === 'tools' ? 'active' : ''}`}
          onClick={() => setActiveTab('tools')}
        >
          <i className="fa-solid fa-wrench"></i> Tools
        </button>
        <button
          type="button"
          className={`tester-tab-btn ${activeTab === 'prompts' ? 'active' : ''}`}
          onClick={() => setActiveTab('prompts')}
        >
          <i className="fa-solid fa-comments"></i> Prompts
        </button>
        <button
          type="button"
          className={`tester-tab-btn ${activeTab === 'resources' ? 'active' : ''}`}
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
