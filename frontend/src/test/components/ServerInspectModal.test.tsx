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

  it('renders nothing when isInspectOpen is false', () => {
    useServerStore.setState({ isInspectOpen: false, inspectServer: null });
    const { container } = render(<ServerInspectModal />);
    expect(container).toBeEmptyDOMElement();
  });

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
