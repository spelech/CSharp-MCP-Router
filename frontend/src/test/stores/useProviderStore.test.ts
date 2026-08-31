/** @requirement UI-100 */

import { describe, it, expect, vi } from 'vitest';
import { useSettingsStore, AuthProviderConfig, SecretProviderConfig } from '../../stores/useSettingsStore';
import { useToastStore } from '../../stores/useToastStore';
import { mockApiResponse, defaultMockData } from '../setup';

describe('useProviderStore (useSettingsStore provider actions)', () => {
  const sampleAuthProvider: AuthProviderConfig = {
    providerName: 'HeaderAuth',
    displayName: 'OIDC / Reverse Proxy Headers',
    isEnabled: true,
    userHeader: 'Remote-User',
    groupsHeader: 'Remote-Groups'
  };

  const sampleSecretProvider: SecretProviderConfig = {
    providerName: 'Vault',
    displayName: 'HashiCorp Vault (KV v2)',
    isEnabled: true,
    configJson: JSON.stringify({
      address: 'http://vault:8200',
      token: 's.mySecretVaultToken',
      mountPath: 'secret/data/'
    })
  };

  /**
   * @requirement AUTH-03
   * @category AUTH
   * @type Positive
   * @description initializes with empty providers
   */
  it('initializes with empty providers', () => {
    const state = useSettingsStore.getState();
    expect(state.authProviders).toEqual([]);
    expect(state.secretProviders).toEqual([]);
  });

  describe('fetchProviders', () => {
    /**
     * @requirement SEC-02
     * @category SEC
     * @type Positive
     * @description successfully loads auth and secret providers
     */
    it('successfully loads auth and secret providers', async () => {
      mockApiResponse('/api/providers/auth', [sampleAuthProvider]);
      mockApiResponse('/api/providers/secrets', [sampleSecretProvider]);

      await useSettingsStore.getState().fetchProviders();

      const state = useSettingsStore.getState();
      expect(state.authProviders).toHaveLength(1);
      expect(state.authProviders[0].providerName).toBe('HeaderAuth');
      expect(state.secretProviders).toHaveLength(1);
      expect(state.secretProviders[0].providerName).toBe('Vault');
    });

    /**
     * @requirement AUTH-03
     * @category AUTH
     * @type Positive
     * @description handles provider fetch warnings gracefully when endpoints are unavailable
     */
    it('handles provider fetch warnings gracefully when endpoints are unavailable', async () => {
      const consoleSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
      mockApiResponse('/api/providers/auth', 'Not initialized', 404);
      mockApiResponse('/api/providers/secrets', 'Not initialized', 404);

      await useSettingsStore.getState().fetchProviders();

      expect(useSettingsStore.getState().authProviders).toEqual([]);
      expect(consoleSpy).toHaveBeenCalled();
      consoleSpy.mockRestore();
    });
  });

  describe('saveAuthProvider', () => {
    /**
     * @requirement AUTH-03
     * @category AUTH
     * @type Positive
     * @description saves auth provider config and refreshes providers
     */
    it('saves auth provider config and refreshes providers', async () => {
      let postBody: any = null;
      mockApiResponse('/api/providers/auth', (_url, options) => {
        if (options?.method === 'POST') {
          postBody = JSON.parse(options.body as string);
          return { success: true };
        }
        return [sampleAuthProvider];
      });

      await useSettingsStore.getState().saveAuthProvider({
        providerName: 'ActiveDirectory',
        displayName: 'Active Directory',
        isEnabled: true
      });

      expect(postBody).toMatchObject({
        providerName: 'ActiveDirectory',
        displayName: 'Active Directory',
        isEnabled: true
      });
    });

    /**
     * @requirement AUTH-03
     * @category AUTH
     * @type Positive
     * @description handles auth provider save error and displays toast
     */
    it('handles auth provider save error and displays toast', async () => {
      mockApiResponse('/api/providers/auth', 'Invalid provider config', 400);

      await expect(
        useSettingsStore.getState().saveAuthProvider({
          providerName: 'InvalidProvider',
          displayName: 'Invalid',
          isEnabled: false
        })
      ).rejects.toThrow();

      expect(useToastStore.getState().toasts.some((t) => t.type === 'error')).toBe(true);
    });
  });

  describe('saveSecretProvider', () => {
    /**
     * @requirement SEC-02
     * @category SEC
     * @type Positive
     * @description saves secret provider preserving Vault token and mount path
     */
    it('saves secret provider preserving Vault token and mount path', async () => {
      let postBody: any = null;
      mockApiResponse('/api/providers/secrets', (_url, options) => {
        if (options?.method === 'POST') {
          postBody = JSON.parse(options.body as string);
          return { success: true };
        }
        return [sampleSecretProvider];
      });

      const vaultConfig = {
        address: 'https://vault.internal:8200',
        token: 's.secureToken12345',
        mountPath: 'homelab/secrets/'
      };

      await useSettingsStore.getState().saveSecretProvider({
        providerName: 'Vault',
        displayName: 'HashiCorp Vault (KV v2)',
        configJson: JSON.stringify(vaultConfig),
        isEnabled: true
      });

      expect(postBody).toMatchObject({
        providerName: 'Vault',
        isEnabled: true
      });
      const parsedConfig = JSON.parse(postBody.configJson);
      expect(parsedConfig.address).toBe('https://vault.internal:8200');
      expect(parsedConfig.token).toBe('s.secureToken12345');
      expect(parsedConfig.mountPath).toBe('homelab/secrets/');
    });

    /**
     * @requirement SEC-02
     * @category SEC
     * @type Positive
     * @description saves Windows Registry and Environment secret providers correctly
     */
    it('saves Windows Registry and Environment secret providers correctly', async () => {
      const savedProviders: any[] = [];
      mockApiResponse('/api/providers/secrets', (_url, options) => {
        if (options?.method === 'POST') {
          savedProviders.push(JSON.parse(options.body as string));
          return { success: true };
        }
        return defaultMockData.secretProviders;
      });

      await useSettingsStore.getState().saveSecretProvider({
        providerName: 'WindowsRegistry',
        displayName: 'Windows Registry (DPAPI)',
        configJson: JSON.stringify({ keyPath: 'HKCU\\Software\\Router' }),
        isEnabled: true
      });

      await useSettingsStore.getState().saveSecretProvider({
        providerName: 'Environment',
        displayName: 'Container Environment',
        configJson: JSON.stringify({ prefix: 'MCP_ENV_' }),
        isEnabled: true
      });

      expect(savedProviders).toHaveLength(2);
      expect(JSON.parse(savedProviders[0].configJson).keyPath).toBe('HKCU\\Software\\Router');
      expect(JSON.parse(savedProviders[1].configJson).prefix).toBe('MCP_ENV_');
    });

    /**
     * @requirement SEC-02
     * @category SEC
     * @type Positive
     * @description handles secret provider save error with toast and throws
     */
    it('handles secret provider save error with toast and throws', async () => {
      mockApiResponse('/api/providers/secrets', 'Failed to connect to Vault', 500);

      await expect(
        useSettingsStore.getState().saveSecretProvider(sampleSecretProvider)
      ).rejects.toThrow();

      expect(useToastStore.getState().toasts.some((t) => t.type === 'error')).toBe(true);
    });
  });
});
