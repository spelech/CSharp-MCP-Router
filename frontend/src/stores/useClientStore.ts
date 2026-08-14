import { create } from 'zustand';
import { showToast } from './useToastStore';
import { RegisteredClient, NewClientResult } from '../shared/types';
import { fetchClientsApi, registerClientApi, deleteClientApi } from '../api/clientApi';

export type { RegisteredClient, NewClientResult };

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
      const data = await fetchClientsApi();
      set({ clients: data || [], isLoadingClients: false });
    } catch (e: any) {
      console.error('Error fetching clients:', e);
      set({ isLoadingClients: false });
    }
  },

  registerClient: async (displayName, scopes) => {
    try {
      const result = await registerClientApi(displayName, scopes);
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
      await deleteClientApi(id);
      showToast('Client deleted successfully', 'success');
      get().fetchClients();
    } catch (e: any) {
      showToast(`Error deleting client: ${e.message}`, 'error');
    }
  },

  openAddClientModal: () => set({ isAddClientOpen: true, createdClientResult: null }),
  closeClientModal: () => set({ isAddClientOpen: false, createdClientResult: null })
}));
