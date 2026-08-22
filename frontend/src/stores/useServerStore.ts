import { create } from 'zustand';
import { showToast } from './useToastStore';
import { confirmAction } from './useConfirmStore';
import { McpServer, InspectCapabilityData, ServerPayload } from '../shared/types';
import {
  fetchServersApi,
  reconnectAllServersApi,
  toggleServerEnabledApi,
  reconnectServerApi,
  createServerApi,
  updateServerApi,
  deleteServerApi,
  inspectServerApi,
} from '../api/serverApi';

export type { McpServer, ServerPayload };

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
  inspectData: InspectCapabilityData;
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
  saveServer: (serverData: ServerPayload | Partial<McpServer>) => Promise<void>;
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
          await reconnectAllServersApi();
        } catch {
          // Ignore error during batch reconnection attempt
        }
      }
      const data = await fetchServersApi();
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
      await toggleServerEnabledApi(id, enabled);
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
      await reconnectServerApi(id);
      get().fetchServers();
    } catch (e: any) {
      showToast(`Failed to reconnect server: ${e.message}`, 'error');
    }
  },

  saveServer: async (serverData) => {
    try {
      const id = serverData.id;
      if (id) {
        await updateServerApi(id, serverData);
        showToast('Server updated successfully', 'success');
      } else {
        await createServerApi(serverData);
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
    const confirmed = await confirmAction({
      title: 'Delete Server',
      message: `Are you sure you want to delete the MCP server '${name}'?`,
      confirmText: 'Delete Server',
      danger: true
    });
    if (!confirmed) return;
    try {
      await deleteServerApi(id);
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
      const data = await inspectServerApi(server.id);
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
