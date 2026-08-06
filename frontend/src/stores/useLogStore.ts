import { create } from 'zustand';
import { apiRequest } from '../utils/api';

export interface LogEntry {
  id: string;
  timestamp: string;
  level: number;
  category: string;
  message: string;
  exception?: string;
}

interface LogStore {
  logs: LogEntry[];
  typeFilter: 'system' | 'rpc';
  levelFilter: 'ALL' | 'INFO' | 'WARNING' | 'ERROR';
  autoScroll: boolean;
  isLoadingLogs: boolean;

  // Actions
  fetchLogs: () => Promise<void>;
  setTypeFilter: (type: 'system' | 'rpc') => void;
  setLevelFilter: (level: 'ALL' | 'INFO' | 'WARNING' | 'ERROR') => void;
  setAutoScroll: (scroll: boolean) => void;
  clearLogs: () => Promise<void>;
}

export const useLogStore = create<LogStore>((set) => ({
  logs: [],
  typeFilter: 'system',
  levelFilter: 'ALL',
  autoScroll: true,
  isLoadingLogs: false,

  fetchLogs: async () => {
    try {
      const data = await apiRequest<LogEntry[]>('/api/logs');
      set({ logs: data || [] });
    } catch (err) {
      console.error('Failed to fetch logs:', err);
    }
  },

  setTypeFilter: (type) => set({ typeFilter: type }),
  setLevelFilter: (level) => set({ levelFilter: level }),
  setAutoScroll: (scroll) => set({ autoScroll: scroll }),

  clearLogs: async () => {
    try {
      await apiRequest('/api/logs', { method: 'DELETE' });
      set({ logs: [] });
    } catch (err) {
      console.error('Failed to clear logs:', err);
    }
  }
}));
