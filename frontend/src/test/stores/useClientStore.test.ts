import { describe, it, expect, vi } from 'vitest';
import { useClientStore, RegisteredClient } from '../../stores/useClientStore';
import { useAppKeyStore, AppKeyItem, AppKeyLimits } from '../../stores/useAppKeyStore';
import { useToastStore } from '../../stores/useToastStore';
import { mockApiResponse } from '../setup';

describe('useClientStore', () => {
  const sampleClient: RegisteredClient = {
    id: 'c-123',
    clientId: 'cursor-ide',
    displayName: 'Cursor IDE',
    isDynamic: false,
    scopes: ['mcp_client']
  };

  /**

   * @requirement UI-01

   * @category UI

   * @type PositiveFeature

   * @description Renders the dashboard and visualizes MCP server states

   */

  it('initializes with default state', () => {
    const state = useClientStore.getState();
    expect(state.clients).toEqual([]);
    expect(state.isLoadingClients).toBe(false);
    expect(state.isAddClientOpen).toBe(false);
    expect(state.createdClientResult).toBeNull();
  });

  describe('fetchClients', () => {
    /**
     * @requirement UI-01
     * @category UI
     * @type PositiveFeature
     * @description Renders the dashboard and visualizes MCP server states
     */
    it('fetches registered clients and updates state', async () => {
      mockApiResponse('/api/clients', [sampleClient]);

      const promise = useClientStore.getState().fetchClients();
      expect(useClientStore.getState().isLoadingClients).toBe(true);

      await promise;

      expect(useClientStore.getState().isLoadingClients).toBe(false);
      expect(useClientStore.getState().clients).toHaveLength(1);
      expect(useClientStore.getState().clients[0].displayName).toBe('Cursor IDE');
    });

    /**

     * @requirement UI-01

     * @category UI

     * @type PositiveFeature

     * @description Renders the dashboard and visualizes MCP server states

     */

    it('handles fetch error gracefully without crashing', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      mockApiResponse('/api/clients', 'Failed to load', 500);

      await useClientStore.getState().fetchClients();

      expect(useClientStore.getState().isLoadingClients).toBe(false);
      expect(useClientStore.getState().clients).toEqual([]);
      consoleSpy.mockRestore();
    });
  });

  describe('registerClient', () => {
    /**
     * @requirement UI-01
     * @category UI
     * @type PositiveFeature
     * @description Renders the dashboard and visualizes MCP server states
     */
    it('creates client with one-time secret result and refreshes list', async () => {
      const createdResult = {
        clientId: 'new-client-uuid',
        clientSecret: 'secret_live_token_12345'
      };
      let postBody: any = null;
      mockApiResponse('/api/clients', (_url, options) => {
        if (options?.method === 'POST') {
          postBody = JSON.parse(options.body as string);
          return createdResult;
        }
        return [sampleClient];
      });

      await useClientStore.getState().registerClient('OpenClaw PC', ['admin', 'mcp_client']);

      expect(postBody).toEqual({
        displayName: 'OpenClaw PC',
        scopes: ['admin', 'mcp_client']
      });
      expect(useClientStore.getState().createdClientResult).toEqual(createdResult);
      expect(useToastStore.getState().toasts.some((t) => t.message.includes('registered successfully'))).toBe(true);
    });

    /**

     * @requirement UI-01

     * @category UI

     * @type PositiveFeature

     * @description Renders the dashboard and visualizes MCP server states

     */

    it('handles register error with toast and propagates error', async () => {
      mockApiResponse('/api/clients', 'Registration failed', 400);

      await expect(
        useClientStore.getState().registerClient('Invalid Client', [])
      ).rejects.toThrow();

      expect(useToastStore.getState().toasts.some((t) => t.type === 'error')).toBe(true);
    });
  });

  describe('deleteClient', () => {
    /**
     * @requirement UI-01
     * @category UI
     * @type PositiveFeature
     * @description Renders the dashboard and visualizes MCP server states
     */
    it('prompts confirmation and deletes client when confirmed', async () => {
      window.confirm = vi.fn(() => true);
      let deleteCalled = false;
      mockApiResponse(/\/api\/clients\/c-123/, (_url, options) => {
        if (options?.method === 'DELETE') {
          deleteCalled = true;
          return { success: true };
        }
        return { success: true };
      });

      await useClientStore.getState().deleteClient('c-123', 'Cursor IDE');

      expect(window.confirm).toHaveBeenCalled();
      expect(deleteCalled).toBe(true);
      expect(useToastStore.getState().toasts.some((t) => t.message.includes('deleted successfully'))).toBe(true);
    });

    /**

     * @requirement UI-01

     * @category UI

     * @type PositiveFeature

     * @description Renders the dashboard and visualizes MCP server states

     */

    it('cancels deletion when user denies confirmation', async () => {
      window.confirm = vi.fn(() => false);
      let deleteCalled = false;
      mockApiResponse(/\/api\/clients\/c-123/, (_url, options) => {
        if (options?.method === 'DELETE') {
          deleteCalled = true;
          return { success: true };
        }
        return { success: true };
      });

      await useClientStore.getState().deleteClient('c-123', 'Cursor IDE');

      expect(window.confirm).toHaveBeenCalled();
      expect(deleteCalled).toBe(false);
    });

    /**

     * @requirement UI-01

     * @category UI

     * @type PositiveFeature

     * @description Renders the dashboard and visualizes MCP server states

     */

    it('handles delete failure with error toast', async () => {
      window.confirm = vi.fn(() => true);
      mockApiResponse(/\/api\/clients\/c-123/, 'Delete failed', 500);

      await useClientStore.getState().deleteClient('c-123', 'Cursor IDE');

      expect(useToastStore.getState().toasts.some((t) => t.type === 'error')).toBe(true);
    });
  });

  describe('modal state', () => {
    /**
     * @requirement UI-01
     * @category UI
     * @type PositiveFeature
     * @description Renders the dashboard and visualizes MCP server states
     */
    it('opens and closes add client modal and resets created result', () => {
      useClientStore.setState({ createdClientResult: { clientId: 'c', clientSecret: 's' } });

      useClientStore.getState().openAddClientModal();
      expect(useClientStore.getState().isAddClientOpen).toBe(true);
      expect(useClientStore.getState().createdClientResult).toBeNull();

      useClientStore.getState().closeClientModal();
      expect(useClientStore.getState().isAddClientOpen).toBe(false);
    });
  });
});

describe('useAppKeyStore', () => {
  const sampleKey: AppKeyItem = {
    id: 'k-1',
    name: 'Cursor Local',
    username: 'admin',
    keyPrefix: 'mcp_live_1234',
    scopes: ['all'],
    createdAt: '2026-08-14T00:00:00Z'
  };

  const sampleLimits: AppKeyLimits = {
    globalMax: 50,
    userMax: 10,
    totalActiveKeys: 5,
    userActiveKeys: 2,
    isLimitReached: false
  };

  /**

   * @requirement UI-01

   * @category UI

   * @type PositiveFeature

   * @description Renders the dashboard and visualizes MCP server states

   */

  it('initializes with default state', () => {
    const state = useAppKeyStore.getState();
    expect(state.appKeys).toEqual([]);
    expect(state.limits).toBeNull();
    expect(state.isLoading).toBe(false);
    expect(state.isCreateModalOpen).toBe(false);
    expect(state.createdResult).toBeNull();
  });

  describe('fetchAppKeys and fetchLimits', () => {
    /**
     * @requirement UI-01
     * @category UI
     * @type PositiveFeature
     * @description Renders the dashboard and visualizes MCP server states
     */
    it('loads app keys and updates store', async () => {
      mockApiResponse('/api/appkeys', [sampleKey]);

      const promise = useAppKeyStore.getState().fetchAppKeys();
      expect(useAppKeyStore.getState().isLoading).toBe(true);

      await promise;

      expect(useAppKeyStore.getState().isLoading).toBe(false);
      expect(useAppKeyStore.getState().appKeys).toEqual([sampleKey]);
    });

    /**

     * @requirement UI-01

     * @category UI

     * @type PositiveFeature

     * @description Renders the dashboard and visualizes MCP server states

     */

    it('loads app key limits', async () => {
      mockApiResponse('/api/appkeys/limits', sampleLimits);

      await useAppKeyStore.getState().fetchLimits();

      expect(useAppKeyStore.getState().limits).toEqual(sampleLimits);
    });
  });

  describe('createAppKey', () => {
    /**
     * @requirement UI-01
     * @category UI
     * @type PositiveFeature
     * @description Renders the dashboard and visualizes MCP server states
     */
    it('creates category-scoped key, captures one-time plaintext key, and refreshes', async () => {
      const createdResult = {
        id: 'new-key-id',
        name: 'SmartHome CLI',
        username: 'admin',
        keyPrefix: 'mcp_live_5678',
        plaintextKey: 'mcp_live_5678_secret_token_123',
        scopes: ['category:smarthome'],
        expiresAt: '2026-09-14T00:00:00Z',
        createdAt: '2026-08-14T00:00:00Z'
      };

      let postBody: any = null;
      mockApiResponse('/api/appkeys', (_url, options) => {
        if (options?.method === 'POST') {
          postBody = JSON.parse(options.body as string);
          return createdResult;
        }
        return [sampleKey];
      });

      await useAppKeyStore.getState().createAppKey({
        name: 'SmartHome CLI',
        scopes: ['category:smarthome'],
        expiresInDays: 30
      });

      expect(postBody).toEqual({
        name: 'SmartHome CLI',
        scopes: ['category:smarthome'],
        expiresInDays: 30
      });
      expect(useAppKeyStore.getState().createdResult).toEqual(createdResult);
      expect(useToastStore.getState().toasts.some((t) => t.message.includes('created successfully'))).toBe(true);
    });

    /**

     * @requirement UI-01

     * @category UI

     * @type PositiveFeature

     * @description Renders the dashboard and visualizes MCP server states

     */

    it('handles create key error with toast and throws', async () => {
      mockApiResponse('/api/appkeys', 'Quota exceeded', 400);

      await expect(
        useAppKeyStore.getState().createAppKey({
          name: 'Exceeded Key',
          scopes: ['all']
        })
      ).rejects.toThrow();

      expect(useToastStore.getState().toasts.some((t) => t.type === 'error')).toBe(true);
    });
  });

  describe('revokeAppKey', () => {
    /**
     * @requirement UI-01
     * @category UI
     * @type PositiveFeature
     * @description Renders the dashboard and visualizes MCP server states
     */
    it('confirms and revokes AppKey and refreshes list', async () => {
      window.confirm = vi.fn(() => true);
      let deleteCalled = false;
      mockApiResponse(/\/api\/appkeys\/k-1/, (_url, options) => {
        if (options?.method === 'DELETE') {
          deleteCalled = true;
          return { success: true };
        }
        return { success: true };
      });

      await useAppKeyStore.getState().revokeAppKey('k-1', 'Cursor Local');

      expect(window.confirm).toHaveBeenCalled();
      expect(deleteCalled).toBe(true);
      expect(useToastStore.getState().toasts.some((t) => t.message.includes('revoked successfully'))).toBe(true);
    });

    /**

     * @requirement UI-01

     * @category UI

     * @type PositiveFeature

     * @description Renders the dashboard and visualizes MCP server states

     */

    it('cancels revocation when confirm is rejected', async () => {
      window.confirm = vi.fn(() => false);
      let deleteCalled = false;
      mockApiResponse(/\/api\/appkeys\/k-1/, (_url, options) => {
        if (options?.method === 'DELETE') {
          deleteCalled = true;
          return { success: true };
        }
        return { success: true };
      });

      await useAppKeyStore.getState().revokeAppKey('k-1', 'Cursor Local');

      expect(deleteCalled).toBe(false);
    });
  });

  describe('modal controls', () => {
    /**
     * @requirement UI-01
     * @category UI
     * @type PositiveFeature
     * @description Renders the dashboard and visualizes MCP server states
     */
    it('opens and closes create modal and clears result', () => {
      useAppKeyStore.setState({ createdResult: { id: 'k', name: 'n', username: 'u', keyPrefix: 'p', plaintextKey: 's', scopes: ['all'], createdAt: '' } });

      useAppKeyStore.getState().openModal();
      expect(useAppKeyStore.getState().isCreateModalOpen).toBe(true);
      expect(useAppKeyStore.getState().createdResult).toBeNull();

      useAppKeyStore.getState().closeModal();
      expect(useAppKeyStore.getState().isCreateModalOpen).toBe(false);
    });
  });
});
