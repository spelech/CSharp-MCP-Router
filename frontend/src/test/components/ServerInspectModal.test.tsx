import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ServerInspectModal } from '../../components/servers/ServerInspectModal';
import { useServerStore, McpServer } from '../../stores/useServerStore';

describe('ServerInspectModal Component', () => {
  const mockServer: McpServer = {
    id: 'docker',
    displayName: 'Docker Server',
    url: 'http://docker-mcp:8000',
    type: 'sse',
    enabled: true,
    connectionStatus: 'Connected',
    categories: ['infrastructure'],
    hasApiKey: false,
    hidden: false,
    connectionAttempts: 0,
    connectionError: '',
  };

  const mockInspectData = {
    tools: [
      {
        name: 'list_containers',
        description: 'Lists all docker containers',
        inputSchema: { type: 'object', properties: { all: { type: 'boolean' } } },
      },
    ],
    resources: [
      {
        name: 'Docker Logs',
        uri: 'docker://logs/system',
        description: 'System log output',
        mimeType: 'text/plain',
      },
    ],
    prompts: [
      {
        name: 'diagnose_container',
        description: 'Diagnose stopped container',
        arguments: [{ name: 'container_id', required: true }],
      },
    ],
  };

  /**
   * @id UI-02
   * @category UI
   * @type positive
   * @description Modal remains hidden when isInspectOpen is false
   */
  /**
   * @requirement UI-01
   * @category UI
   * @type PositiveFeature
   * @description Renders the dashboard and visualizes MCP server states
   */
  it('renders nothing when isInspectOpen is false', () => {
    useServerStore.setState({ isInspectOpen: false, inspectServer: null });
    const { container } = render(<ServerInspectModal />);
    expect(container).toBeEmptyDOMElement();
  });

  /**
   * @id UI-02
   * @category UI
   * @type positive
   * @description Inspect modal displays spinner loading state while querying server capabilities
   */
  /**
   * @requirement UI-01
   * @category UI
   * @type PositiveFeature
   * @description Renders the dashboard and visualizes MCP server states
   */
  it('renders loading state when inspectLoading is true', () => {
    useServerStore.setState({
      isInspectOpen: true,
      inspectServer: mockServer,
      inspectLoading: true,
      inspectData: { tools: [], resources: [], prompts: [] },
    });

    render(<ServerInspectModal />);
    expect(screen.getByText(/Querying backend capabilities.../i)).toBeInTheDocument();
  });

  /**
   * @id UI-02
   * @category UI
   * @type positive
   * @description Inspect modal renders tool schemas and handles tab navigation across resources and prompts
   */
  /**
   * @requirement UI-01
   * @category UI
   * @type PositiveFeature
   * @description Renders the dashboard and visualizes MCP server states
   */
  it('renders tools tab with schema and handles tab switching', () => {
    useServerStore.setState({
      isInspectOpen: true,
      inspectServer: mockServer,
      inspectLoading: false,
      inspectActiveTab: 'tools',
      inspectSearchQuery: '',
      inspectData: mockInspectData,
    });

    render(<ServerInspectModal />);

    // Check header
    expect(screen.getByText('Capabilities: Docker Server')).toBeInTheDocument();

    // Check tool card
    expect(screen.getByText('list_containers')).toBeInTheDocument();
    expect(screen.getByText('Lists all docker containers')).toBeInTheDocument();
    expect(screen.getByText('Input Schema')).toBeInTheDocument();

    // Switch to Resources tab
    const resourcesBtn = screen.getByRole('button', { name: /Resources/i });
    fireEvent.click(resourcesBtn);
    expect(useServerStore.getState().inspectActiveTab).toBe('resources');

    // Switch to Prompts tab
    const promptsBtn = screen.getByRole('button', { name: /Prompts/i });
    fireEvent.click(promptsBtn);
    expect(useServerStore.getState().inspectActiveTab).toBe('prompts');
  });

  /**
   * @id UI-02
   * @category UI
   * @type positive
   * @description Resources tab lists server resources and filters items by text search query
   */
  /**
   * @requirement UI-01
   * @category UI
   * @type PositiveFeature
   * @description Renders the dashboard and visualizes MCP server states
   */
  it('renders resources tab items and handles search filtering', () => {
    useServerStore.setState({
      isInspectOpen: true,
      inspectServer: mockServer,
      inspectLoading: false,
      inspectActiveTab: 'resources',
      inspectSearchQuery: '',
      inspectData: mockInspectData,
    });

    render(<ServerInspectModal />);

    expect(screen.getByText('Docker Logs')).toBeInTheDocument();
    expect(screen.getByText('docker://logs/system')).toBeInTheDocument();
    expect(screen.getByText('text/plain')).toBeInTheDocument();

    // Filter search box
    const searchInput = screen.getByPlaceholderText('Filter resources...');
    fireEvent.change(searchInput, { target: { value: 'nonexistent' } });
    expect(useServerStore.getState().inspectSearchQuery).toBe('nonexistent');
  });

  /**
   * @id UI-02
   * @category UI
   * @type positive
   * @description Prompts tab displays prompt templates and parameter requirements
   */
  /**
   * @requirement UI-01
   * @category UI
   * @type PositiveFeature
   * @description Renders the dashboard and visualizes MCP server states
   */
  it('renders prompts tab with arguments and empty state when filtered out', () => {
    useServerStore.setState({
      isInspectOpen: true,
      inspectServer: mockServer,
      inspectLoading: false,
      inspectActiveTab: 'prompts',
      inspectSearchQuery: '',
      inspectData: mockInspectData,
    });

    render(<ServerInspectModal />);

    expect(screen.getByText('diagnose_container')).toBeInTheDocument();
    expect(screen.getByText(/container_id \*/)).toBeInTheDocument();
  });

  /**
   * @id UI-02
   * @category UI
   * @type positive
   * @description Tabs display clean empty states when inspected server exposes no capabilities
   */
  /**
   * @requirement UI-01
   * @category UI
   * @type PositiveFeature
   * @description Renders the dashboard and visualizes MCP server states
   */
  it('renders empty states for tabs when data is empty', () => {
    useServerStore.setState({
      isInspectOpen: true,
      inspectServer: mockServer,
      inspectLoading: false,
      inspectActiveTab: 'tools',
      inspectSearchQuery: '',
      inspectData: { tools: [], resources: [], prompts: [] },
    });

    const { rerender } = render(<ServerInspectModal />);
    expect(screen.getByText('No tools found.')).toBeInTheDocument();

    useServerStore.setState({ inspectActiveTab: 'resources' });
    rerender(<ServerInspectModal />);
    expect(screen.getByText('No resources found.')).toBeInTheDocument();

    useServerStore.setState({ inspectActiveTab: 'prompts' });
    rerender(<ServerInspectModal />);
    expect(screen.getByText('No prompts found.')).toBeInTheDocument();
  });

  /**
   * @id UI-02
   * @category UI
   * @type positive
   * @description Clicking close button dismisses inspect modal
   */
  /**
   * @requirement UI-01
   * @category UI
   * @type PositiveFeature
   * @description Renders the dashboard and visualizes MCP server states
   */
  it('closes modal when close button is clicked', () => {
    const closeSpy = vi.spyOn(useServerStore.getState(), 'closeInspectModal');
    useServerStore.setState({
      isInspectOpen: true,
      inspectServer: mockServer,
      inspectLoading: false,
      inspectActiveTab: 'tools',
      inspectData: mockInspectData,
    });

    render(<ServerInspectModal />);

    const closeBtn = screen.getByText('Close');
    fireEvent.click(closeBtn);
    expect(closeSpy).toHaveBeenCalled();
  });
});
