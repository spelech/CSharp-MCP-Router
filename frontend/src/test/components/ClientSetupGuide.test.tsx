/** @requirement UI-109 */

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import { ClientSetupGuide } from '../../components/clients/ClientSetupGuide';
import * as serverApi from '../../api/serverApi';
import * as appKeyApi from '../../api/appKeyApi';
import { useToastStore } from '../../stores/useToastStore';

vi.mock('../../api/serverApi');
vi.mock('../../api/appKeyApi');

describe('ClientSetupGuide Component', () => {
  const sampleServers = [
    { id: 'ha', displayName: 'Home Assistant', url: 'http://ha:8086/mcp', enabled: true, hidden: false, type: 'http', categories: [] },
    { id: 'docker', displayName: 'Docker Containers', url: 'http://docker:8000/sse', enabled: true, hidden: false, type: 'sse', categories: [] }
  ];

  const sampleKeys = [
    { id: 'key1', name: 'Work Laptop', username: 'spelech', keyPrefix: 'mcp_live_abc123', keyType: 'personal' as const, scopes: ['all'], createdAt: '2026-08-01' },
    { id: 'key2', name: 'Agent Service', username: 'spelech', keyPrefix: 'mcp_live_xyz789', keyType: 'system' as const, scopes: ['docker'], createdAt: '2026-08-10' }
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(serverApi.fetchServersApi).mockResolvedValue(sampleServers as any);
    vi.mocked(appKeyApi.fetchAppKeysApi).mockResolvedValue(sampleKeys as any);
  });

  it('renders default standard mcpServers configuration with meta mode', async () => {
    render(<ClientSetupGuide />);

    expect(screen.getByText('Client Connection Guide')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText(/mcpServers/i)).toBeInTheDocument();
    });

    // Check default URL has meta=true
    expect(screen.getByText(/\/sse\?meta=true/i)).toBeInTheDocument();
    // Check default server scope is all
    expect(screen.getByTestId('server-scope-select')).toHaveValue('all');
  });

  it('switches between format tabs (Standard, VS Code, Generic SSE)', async () => {
    render(<ClientSetupGuide />);

    await waitFor(() => {
      expect(screen.getByText(/mcpServers/i)).toBeInTheDocument();
    });

    // Switch to VS Code
    const vscodeBtn = screen.getByRole('button', { name: /VS Code/i });
    fireEvent.click(vscodeBtn);
    expect(screen.getByText(/"type":\s*"sse"/i)).toBeInTheDocument();
    expect(screen.getByText(/"mcp\.servers"/i)).toBeInTheDocument();

    // Switch to Generic SSE
    const genericBtn = screen.getByRole('button', { name: /Generic SSE/i });
    fireEvent.click(genericBtn);
    expect(screen.getByText(/sseEndpoint/i)).toBeInTheDocument();
    expect(screen.getByText(/"messageEndpoint":\s*".*\/message\?sessionId=\{sessionId\}"/i)).toBeInTheDocument();
    expect(screen.getByText(/"authHeader":\s*"X-App-Key:\s*mcp_live_YOUR_APP_KEY_HERE"/i)).toBeInTheDocument();
  });

  it('switches server scope from all servers to individual server', async () => {
    render(<ClientSetupGuide />);

    await waitFor(() => {
      expect(screen.getByTestId('server-scope-select')).toBeInTheDocument();
    });

    const serverSelect = screen.getByTestId('server-scope-select');
    fireEvent.change(serverSelect, { target: { value: 'docker' } });

    expect(screen.getByText(/\/docker/i)).toBeInTheDocument();
  });

  it('updates domain when LAN or custom is chosen', async () => {
    render(<ClientSetupGuide />);

    await waitFor(() => {
      expect(screen.getByText(/mcpServers/i)).toBeInTheDocument();
    });

    const lanBtn = screen.getByRole('button', { name: /Local LAN/i });
    fireEvent.click(lanBtn);
    expect(screen.getByText(/10\.0\.0\.10:8026/i)).toBeInTheDocument();

    // Select Custom domain
    const customBtn = screen.getByRole('button', { name: /Custom/i });
    fireEvent.click(customBtn);
    const customInput = screen.getByPlaceholderText(/https:\/\/example\.com/i);
    fireEvent.change(customInput, { target: { value: 'https://my-custom-router.internal:9999' } });
    expect(screen.getByText(/https:\/\/my-custom-router\.internal:9999/i)).toBeInTheDocument();
  });

  it('toggles meta mode when server scope is all', async () => {
    render(<ClientSetupGuide />);

    await waitFor(() => {
      expect(screen.getByText(/\/sse\?meta=true/i)).toBeInTheDocument();
    });

    const metaToggle = screen.getByLabelText(/Meta-Mode/i);
    fireEvent.click(metaToggle);
    expect(screen.getByText(/\/sse\?meta=false/i)).toBeInTheDocument();

    fireEvent.click(metaToggle);
    expect(screen.getByText(/\/sse\?meta=true/i)).toBeInTheDocument();
  });

  it('populates app keys dropdown and injects selected key', async () => {
    render(<ClientSetupGuide />);

    await waitFor(() => {
      expect(screen.getByTestId('app-key-select')).toBeInTheDocument();
    });

    const keySelect = screen.getByTestId('app-key-select');
    // Select the first key
    fireEvent.change(keySelect, { target: { value: 'mcp_live_abc123...' } });
    expect(screen.getByText(/"X-App-Key":\s*"mcp_live_abc123\.\.\."/i)).toBeInTheDocument();

    // Select the second key
    fireEvent.change(keySelect, { target: { value: 'mcp_live_xyz789...' } });
    expect(screen.getByText(/"X-App-Key":\s*"mcp_live_xyz789\.\.\."/i)).toBeInTheDocument();
  });

  it('copies configuration to clipboard and triggers success toast', async () => {
    const writeTextSpy = vi.fn().mockResolvedValue(undefined);
    Object.assign(navigator, {
      clipboard: {
        writeText: writeTextSpy,
      },
    });

    render(<ClientSetupGuide />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Copy JSON|Copy Configuration/i })).toBeInTheDocument();
    });

    const copyBtn = screen.getByRole('button', { name: /Copy JSON|Copy Configuration/i });
    await act(async () => {
      fireEvent.click(copyBtn);
    });

    expect(writeTextSpy).toHaveBeenCalled();
    expect(useToastStore.getState().toasts.some((t) => t.message.includes('Configuration copied to clipboard!'))).toBe(true);
  });
});
