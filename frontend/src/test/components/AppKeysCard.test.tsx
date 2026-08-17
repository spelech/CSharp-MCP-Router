import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import { AppKeysCard } from '../../components/clients/AppKeysCard';
import { useAppKeyStore } from '../../stores/useAppKeyStore';

vi.mock('../../stores/useAppKeyStore', () => ({
  useAppKeyStore: vi.fn(),
}));

describe('AppKeysCard Component', () => {
  const fetchAppKeysMock = vi.fn();
  const fetchLimitsMock = vi.fn();
  const revokeAppKeyMock = vi.fn();
  const openModalMock = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  /**
   * @requirement AUTH-02
   * @category AUTH
   * @type PositiveFeature
   * @description Renders empty state and triggers fetch on mount.
   */
  it('renders empty state when no keys exist', () => {
    (useAppKeyStore as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
      appKeys: [],
      limits: { userActiveKeys: 0, userMax: 5, totalActiveKeys: 0, globalMax: 50, isLimitReached: false },
      fetchAppKeys: fetchAppKeysMock,
      fetchLimits: fetchLimitsMock,
      revokeAppKey: revokeAppKeyMock,
      openModal: openModalMock,
    });

    render(<AppKeysCard />);

    expect(screen.getByText(/LiteLLM-Style App Keys/i)).toBeInTheDocument();
    expect(screen.getByText(/No App Keys active/i)).toBeInTheDocument();
    expect(fetchAppKeysMock).toHaveBeenCalled();
    expect(fetchLimitsMock).toHaveBeenCalled();
  });

  /**
   * @requirement AUTH-02
   * @category AUTH
   * @type PositiveFeature
   * @description Renders keys list, handles copy snippet and key revocation.
   */
  it('renders keys list, copies config snippet, and revokes key', async () => {
    const writeTextSpy = vi.fn().mockResolvedValue(undefined);
    Object.assign(navigator, {
      clipboard: {
        writeText: writeTextSpy,
      },
    });
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {});

    (useAppKeyStore as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
      appKeys: [
        {
          id: 'k1',
          name: 'Active Key',
          keyPrefix: 'mcp_live_123',
          username: 'steve',
          scopes: ['category:smarthome'],
          expiresAt: new Date(Date.now() + 86400000).toISOString(),
          createdAt: new Date().toISOString(),
        },
        {
          id: 'k2',
          name: 'Expired Key',
          keyPrefix: 'mcp_live_999',
          username: 'steve',
          scopes: [],
          expiresAt: '2020-01-01T00:00:00Z',
          createdAt: '2019-12-01T00:00:00Z',
        },
        {
          id: 'k3',
          name: 'Never Expiring Key',
          keyPrefix: 'mcp_live_000',
          username: 'admin',
          scopes: null,
          expiresAt: null,
          createdAt: new Date().toISOString(),
        }
      ],
      limits: { userActiveKeys: 3, userMax: 5, totalActiveKeys: 10, globalMax: 50, isLimitReached: false },
      fetchAppKeys: fetchAppKeysMock,
      fetchLimits: fetchLimitsMock,
      revokeAppKey: revokeAppKeyMock,
      openModal: openModalMock,
    });

    render(<AppKeysCard />);

    expect(screen.getByText('Active Key')).toBeInTheDocument();
    expect(screen.getByText('Expired')).toBeInTheDocument();
    expect(screen.getByText('Never')).toBeInTheDocument();
    expect(screen.getByText('category:smarthome')).toBeInTheDocument();

    // Test Copy Snippet button
    const copyBtns = screen.getAllByTitle(/copy mcp config snippet/i);
    await act(async () => {
      fireEvent.click(copyBtns[0]);
    });
    expect(writeTextSpy).toHaveBeenCalled();
    expect(alertSpy).toHaveBeenCalled();

    // Test Revoke button
    const revokeBtns = screen.getAllByRole('button', { name: /revoke/i });
    await act(async () => {
      fireEvent.click(revokeBtns[0]);
    });
    expect(revokeAppKeyMock).toHaveBeenCalledWith('k1', 'Active Key');

    // Test Create App Key button
    const createBtn = screen.getByRole('button', { name: /create app key/i });
    fireEvent.click(createBtn);
    expect(openModalMock).toHaveBeenCalled();
  });
});
