import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import { IdentityAuthTab } from '../../components/settings/IdentityAuthTab';
import * as settingsApi from '../../api/settingsApi';

describe('IdentityAuthTab Component', () => {
  it('renders Active Directory disabled initially, toggles on and exposes fields', async () => {
    const saveSpy = vi.fn().mockResolvedValue(undefined);
    render(
      <IdentityAuthTab
        providers={[
          { providerName: 'ActiveDirectory', displayName: 'Active Directory', isEnabled: false },
          { providerName: 'PocketID_TinyAuth', displayName: 'PocketID / TinyAuth OIDC', isEnabled: true },
        ]}
        saveAuthProvider={saveSpy}
      />
    );

    expect(screen.getByText('Active Directory / LDAP')).toBeInTheDocument();
    expect(screen.queryByLabelText('LDAP Server Host')).not.toBeInTheDocument();

    const adToggle = document.getElementById('auth-ad-enabled') as HTMLInputElement;
    expect(adToggle).not.toBeChecked();

    await act(async () => {
      fireEvent.click(adToggle);
    });

    expect(adToggle).toBeChecked();
    expect(screen.getByLabelText('LDAP Server Host')).toBeInTheDocument();
    expect(screen.getByLabelText('Port')).toBeInTheDocument();
    expect(screen.getByLabelText('Domain Name')).toBeInTheDocument();
  });

  it('fills LDAP parameters and executes test connection', async () => {
    const testApiSpy = vi.spyOn(settingsApi, 'testLdapConnectionApi').mockResolvedValue({
      success: true,
      message: 'LDAP bind successful to ldap-test:636',
    });

    const saveSpy = vi.fn().mockResolvedValue(undefined);
    render(
      <IdentityAuthTab
        providers={[
          {
            providerName: 'ActiveDirectory',
            displayName: 'Active Directory',
            isEnabled: true,
            configJson: JSON.stringify({
              server: 'ldap-test',
              port: 636,
              useSsl: true,
              domain: 'corp.local',
              baseDn: 'DC=corp,DC=local',
              bindDn: 'CN=admin,DC=corp,DC=local',
              bindPassword: 'adminpassword',
            }),
          },
        ]}
        saveAuthProvider={saveSpy}
      />
    );

    const testBtn = screen.getByRole('button', { name: /test connection/i });
    expect(testBtn).toBeInTheDocument();

    await act(async () => {
      fireEvent.click(testBtn);
    });

    expect(testApiSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        server: 'ldap-test',
        port: 636,
        domain: 'corp.local',
      })
    );

    expect(screen.getByText(/LDAP bind successful/i)).toBeInTheDocument();
  });

  it('saves updated Active Directory configuration JSON', async () => {
    const saveSpy = vi.fn().mockResolvedValue(undefined);
    render(
      <IdentityAuthTab
        providers={[
          {
            providerName: 'ActiveDirectory',
            displayName: 'Active Directory',
            isEnabled: true,
          },
        ]}
        saveAuthProvider={saveSpy}
      />
    );

    const serverInput = screen.getByLabelText('LDAP Server Host');
    fireEvent.change(serverInput, { target: { value: 'ldap.production.local' } });

    const saveBtn = screen.getByRole('button', { name: /save auth config/i });
    await act(async () => {
      fireEvent.click(saveBtn);
    });

    expect(saveSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        providerName: 'ActiveDirectory',
        isEnabled: true,
        configJson: expect.stringContaining('ldap.production.local'),
      })
    );
  });
});
