import { describe, it, expect, vi } from 'vitest';
import { useServerStore, McpServer } from '../../stores/useServerStore';
import { useToastStore } from '../../stores/useToastStore';
import { mockApiResponse } from '../setup';

describe('useServerStore', () => {
  const sampleServer: McpServer = {
    id: 'docker-mcp',
    displayName: 'Docker MCP',
    url: 'http://docker-mcp:8080/sse',
    enabled: true,
    hidden: false,
    type: 'sse',
    categories: ['infrastructure'],
    secretProvider: 'None',
    hasApiKey: false,
    connectionStatus: 'Connected',
    connectionAttempts: 0,
    connectionError: '',
    allowPassThroughAuth: false
  };

  it('initializes with default state', () => {
    const state = useServerStore.getState();
    expect(state.servers).toEqual([]);
    expect(state.isLoadingServers).toBe(false);
    expect(state.searchQuery).toBe('');
    expect(state.sortBy).toBe('status-priority');
    expect(state.groupBy).toBe('none');
    expect(state.currentPage).toBe(1);
    expect(state.pageSize).toBe(6);
    expect(state.collapsedGroups).toEqual([]);
    expect(state.isAddEditOpen).toBe(false);
    expect(state.editingServer).toBeNull();
    expect(state.isInspectOpen).toBe(false);
  });

  describe('fetchServers', () => {
    it('successfully loads servers and updates state', async () => {
      mockApiResponse('/api/servers', [sampleServer]);

      const promise = useServerStore.getState().fetchServers();
      expect(useServerStore.getState().isLoadingServers).toBe(true);

      await promise;

      expect(useServerStore.getState().isLoadingServers).toBe(false);
      expect(useServerStore.getState().servers).toHaveLength(1);
      expect(useServerStore.getState().servers[0].id).toBe('docker-mcp');
    });

    it('triggers batch reconnect when refreshAll is true', async () => {
      let reconnectCalled = false;
      mockApiResponse('/api/servers/reconnect-all', () => {
        reconnectCalled = true;
        return { success: true };
      });
      mockApiResponse('/api/servers', [sampleServer]);

      await useServerStore.getState().fetchServers(true);

      expect(reconnectCalled).toBe(true);
      expect(useServerStore.getState().servers).toHaveLength(1);
    });

    it('handles server fetch errors gracefully and shows error toast', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      mockApiResponse('/api/servers', 'Internal Server Error', 500, 'Internal Server Error');

      await useServerStore.getState().fetchServers();

      expect(useServerStore.getState().isLoadingServers).toBe(false);
      const toasts = useToastStore.getState().toasts;
      expect(toasts.some((t) => t.type === 'error')).toBe(true);
      consoleSpy.mockRestore();
    });
  });

  describe('saveServer', () => {
    it('creates a new server via POST when no id is present', async () => {
      let postPayload: any = null;
      mockApiResponse('/api/servers', (_url, options) => {
        if (options?.method === 'POST') {
          postPayload = JSON.parse(options.body as string);
          return { id: 'new-server', ...postPayload };
        }
        return [sampleServer];
      });

      useServerStore.setState({ isAddEditOpen: true });

      await useServerStore.getState().saveServer({
        displayName: 'New Server',
        url: 'http://newserver:8000/sse',
        type: 'sse',
        categories: ['media'],
        enabled: true,
        hidden: false
      });

      expect(postPayload).toMatchObject({
        displayName: 'New Server',
        url: 'http://newserver:8000/sse'
      });
      expect(useServerStore.getState().isAddEditOpen).toBe(false);
      expect(useServerStore.getState().editingServer).toBeNull();
      const toasts = useToastStore.getState().toasts;
      expect(toasts.some((t) => t.message.includes('added successfully'))).toBe(true);
    });

    it('updates an existing server via PUT when id is present', async () => {
      let putPayload: any = null;
      let targetUrl = '';
      mockApiResponse(/\/api\/servers\/docker-mcp/, (url, options) => {
        targetUrl = url;
        if (options?.method === 'PUT') {
          putPayload = JSON.parse(options.body as string);
          return { success: true, ...putPayload };
        }
        return sampleServer;
      });

      useServerStore.setState({ isAddEditOpen: true, editingServer: sampleServer });

      await useServerStore.getState().saveServer({
        id: 'docker-mcp',
        displayName: 'Updated Docker MCP',
        url: 'http://docker-mcp:8080/sse',
        type: 'sse',
        categories: ['infrastructure']
      });

      expect(targetUrl).toBe('/api/servers/docker-mcp');
      expect(putPayload.displayName).toBe('Updated Docker MCP');
      expect(useServerStore.getState().isAddEditOpen).toBe(false);
      expect(useToastStore.getState().toasts.some((t) => t.message.includes('updated successfully'))).toBe(true);
    });

    it('shows error toast when save fails', async () => {
      mockApiResponse('/api/servers', 'Failed to save', 500, 'Server Error');

      await expect(
        useServerStore.getState().saveServer({ displayName: 'Broken Server' })
      ).rejects.toThrow();

      expect(useToastStore.getState().toasts.some((t) => t.type === 'error')).toBe(true);
    });
  });

  describe('toggleServerEnabled', () => {
    it('sends PUT request to update server enabled state and refreshes', async () => {
      let toggleBody: any = null;
      mockApiResponse(/\/api\/servers\/docker-mcp/, (_url, options) => {
        if (options?.method === 'PUT') {
          toggleBody = JSON.parse(options.body as string);
          return { success: true };
        }
        return sampleServer;
      });

      await useServerStore.getState().toggleServerEnabled('docker-mcp', false);

      expect(toggleBody).toEqual({ enabled: false });
      expect(useToastStore.getState().toasts.some((t) => t.message.includes('disabled successfully'))).toBe(true);
    });

    it('handles toggle failure with error toast', async () => {
      mockApiResponse(/\/api\/servers\/docker-mcp/, 'Error', 500, 'Error');

      await useServerStore.getState().toggleServerEnabled('docker-mcp', true);

      expect(useToastStore.getState().toasts.some((t) => t.type === 'error')).toBe(true);
    });
  });

  describe('reconnectServer', () => {
    it('sends reconnect POST request and shows info toast', async () => {
      useServerStore.setState({ servers: [sampleServer] });
      let reconnectCalled = false;
      mockApiResponse('/api/servers/docker-mcp/reconnect', () => {
        reconnectCalled = true;
        return { success: true };
      });

      await useServerStore.getState().reconnectServer('docker-mcp');

      expect(reconnectCalled).toBe(true);
      expect(useToastStore.getState().toasts.some((t) => t.type === 'info')).toBe(true);
    });

    it('handles reconnect failure with error toast', async () => {
      useServerStore.setState({ servers: [sampleServer] });
      mockApiResponse('/api/servers/docker-mcp/reconnect', 'Reconnect failed', 500);

      await useServerStore.getState().reconnectServer('docker-mcp');

      expect(useToastStore.getState().toasts.some((t) => t.type === 'error')).toBe(true);
    });
  });

  describe('deleteServer', () => {
    it('prompts window.confirm and deletes server when confirmed', async () => {
      window.confirm = vi.fn(() => true);
      let deleteCalled = false;
      mockApiResponse(/\/api\/servers\/docker-mcp/, (_url, options) => {
        if (options?.method === 'DELETE') {
          deleteCalled = true;
          return { success: true };
        }
        return sampleServer;
      });

      await useServerStore.getState().deleteServer('docker-mcp', 'Docker MCP');

      expect(window.confirm).toHaveBeenCalled();
      expect(deleteCalled).toBe(true);
      expect(useToastStore.getState().toasts.some((t) => t.message.includes('deleted successfully'))).toBe(true);
    });

    it('does not send delete request when confirm is cancelled', async () => {
      window.confirm = vi.fn(() => false);
      let deleteCalled = false;
      mockApiResponse(/\/api\/servers\/docker-mcp/, (_url, options) => {
        if (options?.method === 'DELETE') {
          deleteCalled = true;
          return { success: true };
        }
        return sampleServer;
      });

      await useServerStore.getState().deleteServer('docker-mcp', 'Docker MCP');

      expect(window.confirm).toHaveBeenCalled();
      expect(deleteCalled).toBe(false);
    });
  });

  describe('UI state mutations', () => {
    it('updates search query and resets page to 1', () => {
      useServerStore.setState({ currentPage: 3 });
      useServerStore.getState().setSearchQuery('notes');
      expect(useServerStore.getState().searchQuery).toBe('notes');
      expect(useServerStore.getState().currentPage).toBe(1);
    });

    it('updates sortBy and groupBy', () => {
      useServerStore.getState().setSortBy('name-asc');
      expect(useServerStore.getState().sortBy).toBe('name-asc');

      useServerStore.getState().setGroupBy('category');
      expect(useServerStore.getState().groupBy).toBe('category');
    });

    it('updates page and pageSize', () => {
      useServerStore.getState().setCurrentPage(2);
      expect(useServerStore.getState().currentPage).toBe(2);

      useServerStore.getState().setPageSize(12);
      expect(useServerStore.getState().pageSize).toBe(12);
      expect(useServerStore.getState().currentPage).toBe(1);
    });

    it('toggles group collapse state', () => {
      useServerStore.getState().toggleGroupCollapse('group-media');
      expect(useServerStore.getState().collapsedGroups).toContain('group-media');

      useServerStore.getState().toggleGroupCollapse('group-media');
      expect(useServerStore.getState().collapsedGroups).not.toContain('group-media');
    });

    it('manages modal open/close actions', () => {
      useServerStore.getState().openAddModal();
      expect(useServerStore.getState().isAddEditOpen).toBe(true);
      expect(useServerStore.getState().editingServer).toBeNull();

      useServerStore.getState().openEditModal(sampleServer);
      expect(useServerStore.getState().isAddEditOpen).toBe(true);
      expect(useServerStore.getState().editingServer).toEqual(sampleServer);

      useServerStore.getState().closeAddEditModal();
      expect(useServerStore.getState().isAddEditOpen).toBe(false);
      expect(useServerStore.getState().editingServer).toBeNull();
    });
  });

  describe('Inspect Modal', () => {
    it('opens inspect modal and loads server inspection data', async () => {
      const inspectData = {
        tools: [{ name: 'docker__list', description: 'List docker containers' }],
        resources: [{ uri: 'docker://status', name: 'Docker status' }],
        prompts: [{ name: 'docker__diagnose', description: 'Diagnose docker' }]
      };
      mockApiResponse('/api/servers/docker-mcp/inspect', inspectData);

      await useServerStore.getState().openInspectModal(sampleServer);

      const state = useServerStore.getState();
      expect(state.isInspectOpen).toBe(true);
      expect(state.inspectServer).toEqual(sampleServer);
      expect(state.inspectLoading).toBe(false);
      expect(state.inspectData.tools).toHaveLength(1);
      expect(state.inspectData.resources).toHaveLength(1);
      expect(state.inspectData.prompts).toHaveLength(1);
    });

    it('handles inspect failure with error toast', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      mockApiResponse('/api/servers/docker-mcp/inspect', 'Inspect error', 500);

      await useServerStore.getState().openInspectModal(sampleServer);

      expect(useServerStore.getState().inspectLoading).toBe(false);
      expect(useToastStore.getState().toasts.some((t) => t.type === 'error')).toBe(true);
      consoleSpy.mockRestore();
    });

    it('sets inspect active tab and search query', () => {
      useServerStore.getState().setInspectActiveTab('resources');
      expect(useServerStore.getState().inspectActiveTab).toBe('resources');

      useServerStore.getState().setInspectSearchQuery('status');
      expect(useServerStore.getState().inspectSearchQuery).toBe('status');

      useServerStore.getState().closeInspectModal();
      expect(useServerStore.getState().isInspectOpen).toBe(false);
      expect(useServerStore.getState().inspectServer).toBeNull();
    });
  });
});
