import { describe, it, expect, vi } from 'vitest';
import { useClientStore, RegisteredClient } from '../../stores/useClientStore';
import { useAppKeyStore, AppKeyItem, AppKeyLimits, UserQuota } from '../../stores/useAppKeyStore';
import { useToastStore } from '../../stores/useToastStore';
import { useConfirmStore } from '../../stores/useConfirmStore';
import { mockApiResponse } from '../setup';

describe('useClientStore', () => {
  const sampleClient: RegisteredClient = {
    id: 'c-123',
    clientId: 'cursor-ide',
    displayName: 'Cursor IDE',
    clientType: 'confidential',
    redirectUris: ['https://oauth.pstmn.io/v1/callback'],
    grantTypes: ['authorization_code', 'refresh_token', 'client_credentials'],
    isDynamic: false,
    scopes: ['mcp_client'],
    createdAt: '2026-08-30T10:00:00Z',
    expiresAt: null
  };

  /**
   * @requirement AUTH-02
   * @category AUTH
   * @type Positive
   * @description initializes with default state
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
     * @requirement UI-31
     * @category UI
     * @type PositiveFeature
     * @description Fetches registered OAuth clients and updates store state.
     */
    it('fetches registered clients and updates state', async () => {
      mockApiResponse('/api/clients', [sampleClient]);

      const promise = useClientStore.getState().fetchClients();
      expect(useClientStore.getState().isLoadingClients).toBe(true);

      await promise;

      expect(useClientStore.getState().isLoadingClients).toBe(false);
      expect(useClientStore.getState().clients).toHaveLength(1);
      expect(useClientStore.getState().clients[0].displayName).toBe('Cursor IDE');
      expect(useClientStore.getState().clients[0].clientType).toBe('confidential');
    });

    /**
     * @requirement AUTH-02
     * @category AUTH
     * @type Positive
     * @description handles fetch error gracefully without crashing
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
     * @requirement UI-32
     * @category UI
     * @type PositiveFeature
     * @description Registers OAuth client with extended metadata (redirect URIs, grant types, client type, expiration) and captures one-time credentials.
     */
    it('creates client with one-time secret result and refreshes list', async () => {
      const createdResult = {
        id: 'new-client-uuid',
        clientId: 'new-client-uuid',
        clientSecret: 'secret_live_token_12345',
        displayName: 'OpenClaw PC',
        scopes: ['admin', 'mcp_client'],
        redirectUris: ['https://oauth.pstmn.io/v1/callback'],
        grantTypes: ['authorization_code', 'client_credentials'],
        expiresAt: null
      };
      let postBody: any = null;
      mockApiResponse('/api/clients', (_url, options) => {
        if (options?.method === 'POST') {
          postBody = JSON.parse(options.body as string);
          return createdResult;
        }
        return [sampleClient];
      });

      await useClientStore.getState().registerClient(
        'OpenClaw PC',
        ['admin', 'mcp_client'],
        ['https://oauth.pstmn.io/v1/callback'],
        ['authorization_code', 'client_credentials'],
        'confidential',
        30
      );

      expect(postBody).toEqual({
        displayName: 'OpenClaw PC',
        scopes: ['admin', 'mcp_client'],
        redirectUris: ['https://oauth.pstmn.io/v1/callback'],
        grantTypes: ['authorization_code', 'client_credentials'],
        clientType: 'confidential',
        expiresInDays: 30
      });
      expect(useClientStore.getState().createdClientResult).toEqual(createdResult);
      expect(useToastStore.getState().toasts.some((t) => t.message.includes('registered successfully'))).toBe(true);
    });

    /**
     * @requirement AUTH-02
     * @category AUTH
     * @type Positive
     * @description handles register error with toast and propagates error
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
     * @requirement UI-CONFIRM-MODAL
     * @category UI
     * @type PositiveFeature
     * @description Prompts confirmation and deletes client when confirmed.
     */
    it('prompts confirmation and deletes client when confirmed', async () => {
      let deleteCalled = false;
      mockApiResponse(/\/api\/clients\/c-123/, (_url, options) => {
        if (options?.method === 'DELETE') {
          deleteCalled = true;
          return { success: true };
        }
        return { success: true };
      });

      const deletePromise = useClientStore.getState().deleteClient('c-123', 'Cursor IDE');

      expect(useConfirmStore.getState().isOpen).toBe(true);
      expect(useConfirmStore.getState().options.title).toBe('Delete Client');
      expect(useConfirmStore.getState().options.danger).toBe(true);

      useConfirmStore.getState().handleConfirm();
      await deletePromise;

      expect(deleteCalled).toBe(true);
      expect(useToastStore.getState().toasts.some((t) => t.message.includes('deleted successfully'))).toBe(true);
    });

    /**
     * @requirement UI-CONFIRM-MODAL
     * @category UI
     * @type FailClosedGuardrail
     * @description Cancels deletion when user denies confirmation.
     */
    it('cancels deletion when user denies confirmation', async () => {
      let deleteCalled = false;
      mockApiResponse(/\/api\/clients\/c-123/, (_url, options) => {
        if (options?.method === 'DELETE') {
          deleteCalled = true;
          return { success: true };
        }
        return { success: true };
      });

      const deletePromise = useClientStore.getState().deleteClient('c-123', 'Cursor IDE');

      expect(useConfirmStore.getState().isOpen).toBe(true);
      useConfirmStore.getState().handleCancel();
      await deletePromise;

      expect(deleteCalled).toBe(false);
    });

    /**
     * @requirement AUTH-02
     * @category AUTH
     * @type Positive
     * @description handles delete failure with error toast
     */
    it('handles delete failure with error toast', async () => {
      mockApiResponse(/\/api\/clients\/c-123/, 'Delete failed', 500);

      const deletePromise = useClientStore.getState().deleteClient('c-123', 'Cursor IDE');
      useConfirmStore.getState().handleConfirm();
      await deletePromise;

      expect(useToastStore.getState().toasts.some((t) => t.type === 'error')).toBe(true);
    });
  });

  describe('modal state', () => {
    /**
     * @requirement AUTH-02
     * @category AUTH
     * @type Positive
     * @description opens and closes add client modal and resets created result
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
    keyType: 'personal',
    keyPrefix: 'mcp_live_1234',
    scopes: ['all'],
    createdAt: '2026-08-14T00:00:00Z'
  };

  const sampleSystemKey: AppKeyItem = {
    id: 'k-2',
    name: 'CI Service Key',
    username: 'admin',
    keyType: 'system',
    keyPrefix: 'mcp_live_9999',
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

  const sampleQuota: UserQuota = {
    username: 'developer1',
    maxKeys: 12,
    createdAt: '2026-08-22T00:00:00Z',
    updatedAt: '2026-08-22T00:00:00Z'
  };

  /**
   * @requirement AUTH-02
   * @category AUTH
   * @type Positive
   * @description initializes with default state
   */
  it('initializes with default state', () => {
    const state = useAppKeyStore.getState();
    expect(state.appKeys).toEqual([]);
    expect(state.limits).toBeNull();
    expect(state.keyTypeTab).toBe('personal');
    expect(state.userQuotas).toEqual([]);
    expect(state.isLoading).toBe(false);
    expect(state.isLoadingQuotas).toBe(false);
    expect(state.isCreateModalOpen).toBe(false);
    expect(state.createdResult).toBeNull();
  });

  describe('keyTypeTab management', () => {
    /**
     * @requirement AUTH-SYSTEM-APPKEY-SEPARATION
     * @category AUTH
     * @type PositiveFeature
     * @description Switches keyTypeTab between personal and system.
     */
    it('switches keyTypeTab between personal and system', () => {
      useAppKeyStore.getState().setKeyTypeTab('system');
      expect(useAppKeyStore.getState().keyTypeTab).toBe('system');

      useAppKeyStore.getState().setKeyTypeTab('personal');
      expect(useAppKeyStore.getState().keyTypeTab).toBe('personal');
    });
  });

  describe('fetchAppKeys and fetchLimits', () => {
    /**
     * @requirement AUTH-PERSONAL-APPKEY-LIST
     * @category AUTH
     * @type PositiveFeature
     * @description Loads personal AppKeys and updates store.
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
     * @requirement AUTH-SYSTEM-APPKEY-SEPARATION
     * @category AUTH
     * @type PositiveFeature
     * @description Fetches system-filtered app keys via query parameters.
     */
    it('fetches system-filtered app keys via query parameters', async () => {
      mockApiResponse('/api/appkeys', (url) => {
        if (url.includes('keyType=system')) {
          return [sampleSystemKey];
        }
        return [sampleKey];
      });

      await useAppKeyStore.getState().fetchAppKeys('system');

      expect(useAppKeyStore.getState().appKeys).toEqual([sampleSystemKey]);
    });

    /**
     * @requirement AUTH-02
     * @category AUTH
     * @type Positive
     * @description loads app key limits
     */
    it('loads app key limits', async () => {
      mockApiResponse('/api/appkeys/limits', sampleLimits);

      await useAppKeyStore.getState().fetchLimits();

      expect(useAppKeyStore.getState().limits).toEqual(sampleLimits);
    });

    /**
     * @requirement AUTH-02
     * @category AUTH
     * @type Positive
     * @description handles fetch error gracefully without crashing
     */
    it('handles fetch error gracefully without crashing', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      mockApiResponse('/api/appkeys', 'Failed to load', 500);

      await useAppKeyStore.getState().fetchAppKeys();

      expect(useAppKeyStore.getState().isLoading).toBe(false);
      expect(useAppKeyStore.getState().appKeys).toEqual([]);
      consoleSpy.mockRestore();
    });
  });

  describe('createAppKey', () => {
    /**
     * @requirement AUTH-PERSONAL-APPKEY-CREATE
     * @category AUTH
     * @type PositiveFeature
     * @description Creates category-scoped personal key, captures one-time plaintext key, and refreshes.
     */
    it('creates category-scoped key, captures one-time plaintext key, and refreshes', async () => {
      const createdResult = {
        id: 'new-key-id',
        name: 'SmartHome CLI',
        username: 'admin',
        keyType: 'personal' as const,
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
        keyType: 'personal',
        scopes: ['category:smarthome'],
        expiresInDays: 30
      });

      expect(postBody).toEqual({
        name: 'SmartHome CLI',
        keyType: 'personal',
        scopes: ['category:smarthome'],
        expiresInDays: 30
      });
      expect(useAppKeyStore.getState().createdResult).toEqual(createdResult);
      expect(useToastStore.getState().toasts.some((t) => t.message.includes('created successfully'))).toBe(true);
    });

    /**
     * @requirement AUTH-02
     * @category AUTH
     * @type Positive
     * @description handles create key error with toast and throws
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
     * @requirement UI-CONFIRM-MODAL
     * @category UI
     * @type PositiveFeature
     * @description Prompts confirmation modal and revokes AppKey when confirmed.
     */
    it('confirms and revokes AppKey and refreshes list', async () => {
      let deleteCalled = false;
      mockApiResponse(/\/api\/appkeys\/k-1/, (_url, options) => {
        if (options?.method === 'DELETE') {
          deleteCalled = true;
          return { success: true };
        }
        return { success: true };
      });

      const revokePromise = useAppKeyStore.getState().revokeAppKey('k-1', 'Cursor Local');

      expect(useConfirmStore.getState().isOpen).toBe(true);
      expect(useConfirmStore.getState().options.title).toBe('Revoke App Key');
      expect(useConfirmStore.getState().options.danger).toBe(true);

      useConfirmStore.getState().handleConfirm();
      await revokePromise;

      expect(deleteCalled).toBe(true);
      expect(useToastStore.getState().toasts.some((t) => t.message.includes('revoked successfully'))).toBe(true);
    });

    /**
     * @requirement UI-CONFIRM-MODAL
     * @category UI
     * @type FailClosedGuardrail
     * @description Cancels revocation when confirm is rejected.
     */
    it('cancels revocation when confirm is rejected', async () => {
      let deleteCalled = false;
      mockApiResponse(/\/api\/appkeys\/k-1/, (_url, options) => {
        if (options?.method === 'DELETE') {
          deleteCalled = true;
          return { success: true };
        }
        return { success: true };
      });

      const revokePromise = useAppKeyStore.getState().revokeAppKey('k-1', 'Cursor Local');

      expect(useConfirmStore.getState().isOpen).toBe(true);
      useConfirmStore.getState().handleCancel();
      await revokePromise;

      expect(deleteCalled).toBe(false);
    });

    /**
     * @requirement AUTH-02
     * @category AUTH
     * @type Positive
     * @description handles revoke failure with error toast
     */
    it('handles revoke failure with error toast', async () => {
      mockApiResponse(/\/api\/appkeys\/k-1/, 'Revoke failed', 500);

      const revokePromise = useAppKeyStore.getState().revokeAppKey('k-1', 'Cursor Local');
      useConfirmStore.getState().handleConfirm();
      await revokePromise;

      expect(useToastStore.getState().toasts.some((t) => t.type === 'error')).toBe(true);
    });
  });

  describe('user quota management', () => {
    /**
     * @requirement AUTH-02
     * @category AUTH
     * @type Positive
     * @description loads user quotas and updates store
     */
    it('loads user quotas and updates store', async () => {
      mockApiResponse('/api/appkeys/quotas', [sampleQuota]);

      const promise = useAppKeyStore.getState().fetchUserQuotas();
      expect(useAppKeyStore.getState().isLoadingQuotas).toBe(true);

      await promise;

      expect(useAppKeyStore.getState().isLoadingQuotas).toBe(false);
      expect(useAppKeyStore.getState().userQuotas).toEqual([sampleQuota]);
    });

    /**
     * @requirement AUTH-02
     * @category AUTH
     * @type Positive
     * @description handles fetchUserQuotas error gracefully without crashing
     */
    it('handles fetchUserQuotas error gracefully without crashing', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      mockApiResponse('/api/appkeys/quotas', 'Failed to load quotas', 500);

      await useAppKeyStore.getState().fetchUserQuotas();

      expect(useAppKeyStore.getState().isLoadingQuotas).toBe(false);
      expect(useAppKeyStore.getState().userQuotas).toEqual([]);
      consoleSpy.mockRestore();
    });

    /**
     * @requirement AUTH-PERSONAL-APPKEY-QUOTA-OVERRIDE
     * @category AUTH
     * @type PositiveFeature
     * @description Sets custom user quota override and refreshes quota list.
     */
    it('sets user quota override and refreshes quota list', async () => {
      let postBody: any = null;
      mockApiResponse('/api/appkeys/quotas', (_url, options) => {
        if (options?.method === 'POST') {
          postBody = JSON.parse(options.body as string);
          return { success: true, username: 'developer1', maxKeys: 15 };
        }
        return [sampleQuota];
      });

      await useAppKeyStore.getState().setUserQuota('developer1', 15);

      expect(postBody).toEqual({ username: 'developer1', maxKeys: 15 });
      expect(useToastStore.getState().toasts.some((t) => t.message.includes('Quota updated'))).toBe(true);
    });

    /**
     * @requirement AUTH-02
     * @category AUTH
     * @type Positive
     * @description handles setUserQuota error with toast and throws
     */
    it('handles setUserQuota error with toast and throws', async () => {
      mockApiResponse('/api/appkeys/quotas', 'Invalid quota', 400);

      await expect(
        useAppKeyStore.getState().setUserQuota('developer1', -1)
      ).rejects.toThrow();

      expect(useToastStore.getState().toasts.some((t) => t.type === 'error')).toBe(true);
    });

    /**
     * @requirement UI-CONFIRM-MODAL
     * @category UI
     * @type PositiveFeature
     * @description Prompts confirmation modal and resets user quota when confirmed.
     */
    it('prompts confirmation modal and resets user quota when confirmed', async () => {
      let deleteCalled = false;
      mockApiResponse(/\/api\/appkeys\/quotas\/developer1/, (_url, options) => {
        if (options?.method === 'DELETE') {
          deleteCalled = true;
          return { success: true, username: 'developer1' };
        }
        return { success: true };
      });
      mockApiResponse('/api/appkeys/quotas', []);

      const deletePromise = useAppKeyStore.getState().deleteUserQuota('developer1');

      expect(useConfirmStore.getState().isOpen).toBe(true);
      expect(useConfirmStore.getState().options.title).toBe('Reset User Quota');
      expect(useConfirmStore.getState().options.danger).toBe(true);

      useConfirmStore.getState().handleConfirm();
      await deletePromise;

      expect(deleteCalled).toBe(true);
      expect(useToastStore.getState().toasts.some((t) => t.message.includes('Quota reset'))).toBe(true);
    });

    /**
     * @requirement UI-CONFIRM-MODAL
     * @category UI
     * @type FailClosedGuardrail
     * @description Cancels quota reset when user denies confirmation.
     */
    it('cancels quota reset when user denies confirmation', async () => {
      let deleteCalled = false;
      mockApiResponse(/\/api\/appkeys\/quotas\/developer1/, (_url, options) => {
        if (options?.method === 'DELETE') {
          deleteCalled = true;
          return { success: true };
        }
        return { success: true };
      });

      const deletePromise = useAppKeyStore.getState().deleteUserQuota('developer1');

      expect(useConfirmStore.getState().isOpen).toBe(true);
      useConfirmStore.getState().handleCancel();
      await deletePromise;

      expect(deleteCalled).toBe(false);
    });

    /**
     * @requirement AUTH-02
     * @category AUTH
     * @type Positive
     * @description handles deleteUserQuota failure with error toast
     */
    it('handles deleteUserQuota failure with error toast', async () => {
      mockApiResponse(/\/api\/appkeys\/quotas\/developer1/, 'Delete quota failed', 500);

      const deletePromise = useAppKeyStore.getState().deleteUserQuota('developer1');
      useConfirmStore.getState().handleConfirm();
      await deletePromise;

      expect(useToastStore.getState().toasts.some((t) => t.type === 'error')).toBe(true);
    });
  });

  describe('modal controls', () => {
    /**
     * @requirement AUTH-02
     * @category AUTH
     * @type Positive
     * @description opens and closes create modal and clears result
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
