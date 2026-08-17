import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import { GeneralTab } from '../../components/settings/GeneralTab';
import { ProvidersTab } from '../../components/settings/ProvidersTab';
import { CustomFilesTab } from '../../components/settings/CustomFilesTab';
import { AccessControlTab } from '../../components/settings/AccessControlTab';
import { BackupsTab } from '../../components/settings/BackupsTab';

describe('Modular Settings Tab Components', () => {
  /**
   * @requirement UI-01
   * @category UI
   * @type PositiveFeature
   * @description Renders the dashboard and visualizes MCP server states
   */
  it('renders GeneralTab and triggers save', async () => {
    const saveSpy = vi.fn().mockResolvedValue(true);
    render(
      <GeneralTab
        settings={{
          embeddingProvider: 'local',
          embeddingModelDir: 'data/models',
          embeddingApiUrl: '',
          embeddingApiModel: 'all-MiniLM-L6-v2',
          embeddingApiKey: '',
        }}
        saveEmbeddingSettings={saveSpy}
      />
    );

    expect(screen.getByText('Semantic Search Settings')).toBeInTheDocument();
    const saveBtn = screen.getByRole('button', { name: /save settings/i });
    await act(async () => {
      fireEvent.click(saveBtn);
    });
    expect(saveSpy).toHaveBeenCalled();
  });

  /**

   * @requirement UI-01

   * @category UI

   * @type PositiveFeature

   * @description Renders the dashboard and visualizes MCP server states

   */

  it('renders IdentityAuthTab and SecretProvidersTab inside ProvidersTab', () => {
    const saveAuthSpy = vi.fn().mockResolvedValue(undefined);
    const saveSecretSpy = vi.fn().mockResolvedValue(undefined);

    render(
      <ProvidersTab
        authProviders={[
          { providerName: 'ActiveDirectory', displayName: 'Active Directory', isEnabled: false },
          { providerName: 'HeaderAuth', displayName: 'OIDC / Reverse Proxy Headers', isEnabled: true },
        ]}
        secretProviders={[
          { providerName: 'Vault', displayName: 'HashiCorp Vault (KV v2)', isEnabled: true, configJson: '{}' },
        ]}
        saveAuthProvider={saveAuthSpy}
        saveSecretProvider={saveSecretSpy}
      />
    );

    expect(screen.getByText('Identity & Auth Providers')).toBeInTheDocument();
    expect(screen.getByText('Secret Providers')).toBeInTheDocument();
  });

  /**

   * @requirement UI-01

   * @category UI

   * @type PositiveFeature

   * @description Renders the dashboard and visualizes MCP server states

   */

  it('renders CustomFilesTab and triggers modal open and delete', async () => {
    const openModalSpy = vi.fn().mockResolvedValue(undefined);
    const deleteSpy = vi.fn().mockResolvedValue(undefined);

    render(
      <CustomFilesTab
        customFiles={[
          { type: 'prompts', name: 'test-prompt.json', sizeBytes: 1024, lastModified: '2026-08-14T00:00:00Z' },
        ]}
        openCustomFileModal={openModalSpy}
        deleteCustomFile={deleteSpy}
      />
    );

    expect(screen.getByText('Prompts & Resources File Manager')).toBeInTheDocument();
    expect(screen.getByText('test-prompt.json')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /create file/i }));
    expect(openModalSpy).toHaveBeenCalled();

    fireEvent.click(screen.getByRole('button', { name: /edit/i }));
    expect(openModalSpy).toHaveBeenCalledWith(expect.objectContaining({ name: 'test-prompt.json' }));

    fireEvent.click(screen.getByRole('button', { name: /delete/i }));
    expect(deleteSpy).toHaveBeenCalledWith('prompts', 'test-prompt.json');
  });

  /**

   * @requirement UI-01

   * @category UI

   * @type PositiveFeature

   * @description Renders the dashboard and visualizes MCP server states

   */

  it('renders AccessControlTab with policies and mappings', () => {
    const openPolicySpy = vi.fn();
    const deletePolicySpy = vi.fn().mockResolvedValue(undefined);
    const openMappingSpy = vi.fn();
    const deleteMappingSpy = vi.fn().mockResolvedValue(undefined);

    render(
      <AccessControlTab
        policies={[{ id: 'p1', targetId: 'server:ha', requiredGroup: 'admins', isAllowed: true }]}
        mappings={[{ id: 'm1', externalId: 'ext_admin', internalGroup: 'Administrators' }]}
        openPolicyModal={openPolicySpy}
        deletePolicy={deletePolicySpy}
        openMappingModal={openMappingSpy}
        deleteMapping={deleteMappingSpy}
      />
    );

    expect(screen.getByText('Access Control Policies')).toBeInTheDocument();
    expect(screen.getByText('Group & SID Mappings')).toBeInTheDocument();
    expect(screen.getByText('server:ha')).toBeInTheDocument();
    expect(screen.getByText('ext_admin')).toBeInTheDocument();
  });

  /**

   * @requirement UI-01

   * @category UI

   * @type PositiveFeature

   * @description Renders the dashboard and visualizes MCP server states

   */

  it('renders BackupsTab', () => {
    render(<BackupsTab />);
    expect(screen.getByText('Database & System Maintenance')).toBeInTheDocument();
  });
});
