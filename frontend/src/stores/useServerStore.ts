import { create } from 'zustand';
import { apiRequest } from '../utils/api';
import { showToast } from './useToastStore';

export interface McpServer {
  id: string;
  displayName: string;
  url: string;
  enabled: boolean;
  hidden: boolean;
  type: string;
  categories: string[];
  secretProvider?: string;
  secretItemKey?: string;
  authShape?: string;
  customHeaderName?: string;
  headersJson?: string;
  hasApiKey: boolean;
  apiKey?: string; // fallback / input
  connectionStatus: string;
  connectionAttempts: number;
  connectionError: string;
}

interface ServerStore {
  servers: McpServer[];
  isLoadingServers: boolean;
  searchQuery: string;
  sortBy: string;
  groupBy: string;
  currentPage: number;
  pageSize: number | 'all';
  collapsedGroups: string[];

  // Modal visibility
  isAddEditOpen: boolean;
  editingServer: McpServer | null; // null for Add

  // Inspect Modal
  isInspectOpen: boolean;
  inspectServer: McpServer | null;
  inspectData: {
    tools: any[];
    resources: any[];
    prompts: any[];
  };
  inspectLoading: boolean;
  inspectActiveTab: 'tools' | 'resources' | 'prompts';
  inspectSearchQuery: string;

  // Actions
  fetchServers: (refreshAll?: boolean) => Promise<void>;
  setSearchQuery: (q: string) => void;
  setSortBy: (s: string) => void;
  setGroupBy: (g: string) => void;
  setCurrentPage: (p: number) => void;
  setPageSize: (size: number | 'all') => void;
  toggleGroupCollapse: (groupId: string) => void;

  // Crud
  toggleServerEnabled: (id: string, enabled: boolean) => Promise<void>;
  reconnectServer: (id: string) => Promise<void>;
  saveServer: (serverData: Partial<McpServer>) => Promise<void>;
  deleteServer: (id: string, name: string) => Promise<void>;

  // Modals actions
  openAddModal: () => void;
  openEditModal: (server: McpServer) => void;
  closeAddEditModal: () => void;

  // Inspect actions
  openInspectModal: (server: McpServer) => Promise<void>;
  closeInspectModal: () => void;
  setInspectActiveTab: (tab: 'tools' | 'resources' | 'prompts') => void;
  setInspectSearchQuery: (q: string) => void;
}

export const useServerStore = create<ServerStore>((set, get) => ({
  servers: [],
  isLoadingServers: false,
  searchQuery: '',
  sortBy: 'status-priority',
  groupBy: 'none',
  currentPage: 1,
  pageSize: 6,
  collapsedGroups: [],

  isAddEditOpen: false,
  editingServer: null,

  isInspectOpen: false,
  inspectServer: null,
  inspectData: { tools: [], resources: [], prompts: [] },
  inspectLoading: false,
  inspectActiveTab: 'tools',
  inspectSearchQuery: '',

  fetchServers: async (refreshAll = false) => {
    set({ isLoadingServers: true });
    try {
      if (refreshAll) {
        try {
          await apiRequest('/api/servers/reconnect-all', { method: 'POST' });
        } catch {}
      }
      const data = await apiRequest<McpServer[]>('/api/servers');
      set({ servers: data || [], isLoadingServers: false });
    } catch (e: any) {
      console.error('Error fetching servers:', e);
      set({ isLoadingServers: false });
      showToast(`Error loading servers: ${e.message}`, 'error');
    }
  },

  setSearchQuery: (q) => set({ searchQuery: q, currentPage: 1 }),
  setSortBy: (s) => set({ sortBy: s, currentPage: 1 }),
  setGroupBy: (g) => set({ groupBy: g, currentPage: 1 }),
  setCurrentPage: (p) => set({ currentPage: p }),
  setPageSize: (size) => set({ pageSize: size, currentPage: 1 }),
  toggleGroupCollapse: (groupId) => set((state) => {
    const collapsed = state.collapsedGroups.includes(groupId)
      ? state.collapsedGroups.filter(id => id !== groupId)
      : [...state.collapsedGroups, groupId];
    return { collapsedGroups: collapsed };
  }),

  toggleServerEnabled: async (id, enabled) => {
    try {
      await apiRequest(`/api/servers/${id}`, {
        method: 'PUT',
        body: { enabled }
      });
      showToast(`Server ${enabled ? 'enabled' : 'disabled'} successfully`, 'success');
      get().fetchServers();
    } catch (e: any) {
      showToast(`Failed to toggle server state: ${e.message}`, 'error');
    }
  },

  reconnectServer: async (id) => {
    try {
      const server = get().servers.find(s => s.id === id);
      showToast(`Triggering reconnection for ${server?.displayName || id}...`, 'info');
      await apiRequest(`/api/servers/${id}/reconnect`, { method: 'POST' });
      get().fetchServers();
    } catch (e: any) {
      showToast(`Failed to reconnect server: ${e.message}`, 'error');
    }
  },

  saveServer: async (serverData) => {
    try {
      const id = serverData.id;
      if (id) {
        await apiRequest(`/api/servers/${id}`, {
          method: 'PUT',
          body: serverData
        });
        showToast('Server updated successfully', 'success');
      } else {
        await apiRequest('/api/servers', {
          method: 'POST',
          body: serverData
        });
        showToast('Server added successfully', 'success');
      }
      set({ isAddEditOpen: false, editingServer: null });
      get().fetchServers();
    } catch (e: any) {
      showToast(`Error saving server: ${e.message}`, 'error');
      throw e;
    }
  },

  deleteServer: async (id, name) => {
    if (!window.confirm(`Are you sure you want to delete the MCP server '${name}'?`)) return;
    try {
      await apiRequest(`/api/servers/${id}`, { method: 'DELETE' });
      showToast('Server deleted successfully', 'success');
      get().fetchServers();
    } catch (e: any) {
      showToast(`Error deleting server: ${e.message}`, 'error');
    }
  },

  openAddModal: () => set({ isAddEditOpen: true, editingServer: null }),
  openEditModal: (server) => set({ isAddEditOpen: true, editingServer: server }),
  closeAddEditModal: () => set({ isAddEditOpen: false, editingServer: null }),

  openInspectModal: async (server) => {
    set({
      isInspectOpen: true,
      inspectServer: server,
      inspectLoading: true,
      inspectActiveTab: 'tools',
      inspectSearchQuery: '',
      inspectData: { tools: [], resources: [], prompts: [] }
    });

    try {
      const data = await apiRequest(`/api/servers/${server.id}/inspect`);
      set({
        inspectData: data || { tools: [], resources: [], prompts: [] },
        inspectLoading: false
      });
    } catch (e: any) {
      console.error('Inspect error:', e);
      set({ inspectLoading: false });
      showToast(`Failed to inspect server: ${e.message}`, 'error');
    }
  },

  closeInspectModal: () => set({ isInspectOpen: false, inspectServer: null }),
  setInspectActiveTab: (tab) => set({ inspectActiveTab: tab }),
  setInspectSearchQuery: (q) => set({ inspectSearchQuery: q })
}));
