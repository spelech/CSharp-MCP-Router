import { create } from 'zustand';
import { apiRequest } from '../utils/api';

interface UserInfo {
  authenticated: boolean;
  username?: string;
  name?: string;
  email?: string;
  groups?: string[];
}

interface UserStore {
  user: UserInfo | null;
  version: string;
  isLoadingUser: boolean;
  loadUser: () => Promise<void>;
  loadVersion: () => Promise<void>;
}

export const useUserStore = create<UserStore>((set) => ({
  user: null,
<<<<<<< HEAD
  version: '4.5.9', // fallback default
=======
  version: '4.6.0', // fallback default
>>>>>>> 1230f47 (feat(identity): implement cross-platform Active Directory SID resolution via LDAP)
  isLoadingUser: false,
  loadUser: async () => {
    set({ isLoadingUser: true });
    try {
      const data = await apiRequest<UserInfo>('/api/me');
      set({ user: data, isLoadingUser: false });
    } catch (e) {
      console.error('Error loading user profile:', e);
      set({ user: { authenticated: false }, isLoadingUser: false });
    }
  },
  loadVersion: async () => {
    try {
      const data = await apiRequest<{ version: string }>('/health');
      if (data && data.version) {
        set({ version: data.version });
      }
    } catch (e) {
      console.error('Error loading version:', e);
    }
  }
}));
