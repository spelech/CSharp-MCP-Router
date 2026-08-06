import { create } from 'zustand';
import { apiRequest } from '../utils/api';
import { showToast } from './useToastStore';

export interface RegisteredClient {
  id: string;
  clientId: string;
  displayName: string;
  isDynamic: boolean;
  scopes?: string[];
}

export interface NewClientResult {
  clientId: string;
  clientSecret: string;
}

interface ClientStore {
  clients: RegisteredClient[];
  isLoadingClients: boolean;
  isAddClientOpen: boolean;
  createdClientResult: NewClientResult | null;

  fetchClients: () => Promise<void>;
  registerClient: (displayName: string, scopes: string[]) => Promise<void>;
  deleteClient: (id: string, name: string) => Promise<void>;

  openAddClientModal: () => void;
  closeClientModal: () => void;
}

export const useClientStore = create<ClientStore>((set, get) => ({
  clients: [],
  isLoadingClients: false,
  isAddClientOpen: false,
  createdClientResult: null,

  fetchClients: async () => {
    set({ isLoadingClients: true });
    try {
      const data = await apiRequest<RegisteredClient[]>('/api/clients');
      set({ clients: data || [], isLoadingClients: false });
    } catch (e: any) {
      console.error('Error fetching clients:', e);
      set({ isLoadingClients: false });
    }
  },

  registerClient: async (displayName, scopes) => {
    try {
      const result = await apiRequest<NewClientResult>('/api/clients', {
        method: 'POST',
        body: { displayName, scopes }
      });
      set({ createdClientResult: result });
      showToast('Client registered successfully', 'success');
      get().fetchClients();
    } catch (e: any) {
      showToast(`Error registering client: ${e.message}`, 'error');
      throw e;
    }
  },

  deleteClient: async (id, name) => {
    if (!window.confirm(`Are you sure you want to delete the registered client '${name}'?`)) return;
    try {
      await apiRequest(`/api/clients/${id}`, { method: 'DELETE' });
      showToast('Client deleted successfully', 'success');
      get().fetchClients();
    } catch (e: any) {
      showToast(`Error deleting client: ${e.message}`, 'error');
    }
  },

  openAddClientModal: () => set({ isAddClientOpen: true, createdClientResult: null }),
  closeClientModal: () => set({ isAddClientOpen: false, createdClientResult: null })
}));
