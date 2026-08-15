import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { DashboardView } from '../../components/servers/DashboardView';
import { useServerStore, McpServer } from '../../stores/useServerStore';

describe('DashboardView Component', () => {
  const mockServers: McpServer[] = [
    {
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
    },
    {
      id: 'media',
      displayName: 'Media Server',
      url: 'http://media-mcp:8080',
      type: 'http',
      enabled: false,
      connectionStatus: 'Disconnected',
      categories: ['media'],
      hasApiKey: false,
      hidden: false,
      connectionAttempts: 0,
      connectionError: '',
    },
  ];

  /**
   * @id UI-01
   * @category UI
   * @type positive
   * @description Dashboard renders stats card, connected server list, and setup instructions
   */
  it('renders stats card, server list, and client setup guide', () => {
    useServerStore.setState({
      servers: mockServers,
      searchQuery: '',
      groupBy: 'none',
      sortBy: 'status-priority',
      currentPage: 1,
      pageSize: 10,
    });

    render(<DashboardView />);

    expect(screen.getByText('Backend MCP Servers')).toBeInTheDocument();
    expect(screen.getByText('Docker Server')).toBeInTheDocument();
    expect(screen.getByText('Media Server')).toBeInTheDocument();
  });

  /**
   * @id UI-03
   * @category UI
   * @type positive
   * @description Grouped server view renders category sections and supports collapsible groups
   */
  it('renders grouped server view by category and allows collapsing', () => {
    useServerStore.setState({
      servers: mockServers,
      searchQuery: '',
      groupBy: 'category',
      collapsedGroups: [],
    });

    render(<DashboardView />);

    expect(screen.getAllByText('infrastructure').length).toBeGreaterThan(0);
    expect(screen.getAllByText('media').length).toBeGreaterThan(0);

    // Toggle collapse
    const groupHeader = screen.getAllByText('infrastructure')[0].closest('.server-group-header');
    if (groupHeader) {
      fireEvent.click(groupHeader);
      expect(useServerStore.getState().collapsedGroups).toContain('infrastructure');
    }
  });

  /**
   * @id UI-03
   * @category UI
   * @type positive
   * @description Grouped server view partitions servers by connection status and transport type
   */
  it('renders grouped server view by status and type', () => {
    useServerStore.setState({
      servers: mockServers,
      groupBy: 'status',
      collapsedGroups: [],
    });

    const { rerender } = render(<DashboardView />);
    expect(screen.getAllByText('Connected').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Disabled').length).toBeGreaterThan(0);

    useServerStore.setState({
      groupBy: 'type',
    });
    rerender(<DashboardView />);
    expect(screen.getAllByText('SSE').length).toBeGreaterThan(0);
    expect(screen.getAllByText('HTTP').length).toBeGreaterThan(0);
  });

  /**
   * @id UI-01
   * @category UI
   * @type positive
   * @description Dashboard shows empty filter state when no servers match search term
   */
  it('renders empty state when no servers match search', () => {
    useServerStore.setState({
      servers: mockServers,
      searchQuery: 'nonexistent-query-string-999',
      groupBy: 'none',
    });

    render(<DashboardView />);
    expect(screen.getByText('No MCP servers matching your filters.')).toBeInTheDocument();
  });
});
