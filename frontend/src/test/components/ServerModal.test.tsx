import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import { ServerModal } from '../../components/servers/ServerModal';
import { useServerStore, McpServer } from '../../stores/useServerStore';

describe('ServerModal component', () => {
  const existingServer: McpServer = {
    id: 'server-1',
    displayName: 'Home Assistant MCP',
    url: 'http://ha-mcp:8123/sse',
    enabled: true,
    hidden: false,
    type: 'sse',
    categories: ['smarthome', 'sensors'],
    secretProvider: 'Vault',
    secretItemKey: 'ha/api-token',
    authShape: 'custom-header',
    customHeaderName: 'X-HA-Access',
    hasApiKey: true,
    connectionStatus: 'Connected',
    connectionAttempts: 0,
    connectionError: ''
  };

  /**

   * @requirement UI-01

   * @category UI

   * @type PositiveFeature

   * @description Renders the dashboard and visualizes MCP server states

   */

  it('renders nothing when isAddEditOpen is false', () => {
    useServerStore.setState({ isAddEditOpen: false, editingServer: null });
    const { container } = render(<ServerModal />);
    expect(container.firstChild).toBeNull();
  });

  /**

   * @requirement UI-01

   * @category UI

   * @type PositiveFeature

   * @description Renders the dashboard and visualizes MCP server states

   */

  it('renders Add MCP Server form with default values when in add mode', () => {
    useServerStore.setState({ isAddEditOpen: true, editingServer: null });
    render(<ServerModal />);

    expect(screen.getByText('Add MCP Server')).toBeInTheDocument();
    expect(screen.getByLabelText('Display Name')).toHaveValue('');
    expect(screen.getByLabelText('Transport Type')).toHaveValue('sse');
    expect(screen.getByLabelText('Category')).toHaveValue('infrastructure');
    expect(screen.getByLabelText('Connection URL')).toHaveValue('');
    expect(screen.getByLabelText('Secret Provider')).toHaveValue('None');
    expect(screen.getByLabelText('Auth Token Format / Shape')).toHaveValue('bearer');
    expect((document.getElementById('server-enabled') as HTMLInputElement)).toBeChecked();
    expect((document.getElementById('server-hidden') as HTMLInputElement)).not.toBeChecked();
  });

  /**

   * @requirement UI-01

   * @category UI

   * @type PositiveFeature

   * @description Renders the dashboard and visualizes MCP server states

   */

  it('renders Edit MCP Server form populated with server details when editing', () => {
    useServerStore.setState({ isAddEditOpen: true, editingServer: existingServer });
    render(<ServerModal />);

    expect(screen.getByText('Edit MCP Server')).toBeInTheDocument();
    expect(screen.getByLabelText('Display Name')).toHaveValue('Home Assistant MCP');
    expect(screen.getByLabelText('Transport Type')).toHaveValue('sse');
    expect(screen.getByLabelText('Category')).toHaveValue('smarthome, sensors');
    expect(screen.getByLabelText('Connection URL')).toHaveValue('http://ha-mcp:8123/sse');
    expect(screen.getByLabelText('Secret Provider')).toHaveValue('Vault');
    expect(screen.getByLabelText('Secret Key / Item Name')).toHaveValue('ha/api-token');
    expect(screen.getByLabelText('Auth Token Format / Shape')).toHaveValue('custom-header');
    expect(screen.getByLabelText('Custom Header / Query Name')).toHaveValue('X-HA-Access');
  });

  /**

   * @requirement UI-01

   * @category UI

   * @type PositiveFeature

   * @description Renders the dashboard and visualizes MCP server states

   */

  it('switches to connection command when STDIO transport type is selected', async () => {
    useServerStore.setState({ isAddEditOpen: true, editingServer: null });
    render(<ServerModal />);

    const transportSelect = screen.getByLabelText('Transport Type');
    fireEvent.change(transportSelect, { target: { value: 'stdio' } });

    expect(screen.getByLabelText('Connection Command')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('e.g. node /app/mock_stdio.js')).toBeInTheDocument();

    // Switch back to HTTP
    fireEvent.change(transportSelect, { target: { value: 'http' } });
    expect(screen.getByLabelText('Connection URL')).toBeInTheDocument();
  });

  /**

   * @requirement UI-01

   * @category UI

   * @type PositiveFeature

   * @description Renders the dashboard and visualizes MCP server states

   */

  it('shows custom header input when auth shape is custom-header or query', () => {
    useServerStore.setState({ isAddEditOpen: true, editingServer: null });
    render(<ServerModal />);

    const authShapeSelect = screen.getByLabelText('Auth Token Format / Shape');
    expect(screen.queryByLabelText('Custom Header / Query Name')).toBeNull();

    fireEvent.change(authShapeSelect, { target: { value: 'custom-header' } });
    expect(screen.getByLabelText('Custom Header / Query Name')).toBeInTheDocument();

    fireEvent.change(authShapeSelect, { target: { value: 'query' } });
    expect(screen.getByLabelText('Custom Header / Query Name')).toBeInTheDocument();

    fireEvent.change(authShapeSelect, { target: { value: 'bearer' } });
    expect(screen.queryByLabelText('Custom Header / Query Name')).toBeNull();
  });

  /**

   * @requirement UI-01

   * @category UI

   * @type PositiveFeature

   * @description Renders the dashboard and visualizes MCP server states

   */

  it('closes modal when cancel button or close X is clicked', () => {
    const closeSpy = vi.fn();
    useServerStore.setState({ isAddEditOpen: true, editingServer: null, closeAddEditModal: closeSpy });
    render(<ServerModal />);

    const cancelBtn = screen.getByRole('button', { name: /cancel/i });
    fireEvent.click(cancelBtn);
    expect(closeSpy).toHaveBeenCalledTimes(1);

    const closeBtn = document.querySelector('.btn-close');
    fireEvent.click(closeBtn!);
    expect(closeSpy).toHaveBeenCalledTimes(2);
  });

  /**

   * @requirement UI-01

   * @category UI

   * @type PositiveFeature

   * @description Renders the dashboard and visualizes MCP server states

   */

  it('submits form with correctly formatted payload including trimmed categories', async () => {
    const saveSpy = vi.fn().mockResolvedValue(undefined);
    useServerStore.setState({ isAddEditOpen: true, editingServer: null, saveServer: saveSpy });
    render(<ServerModal />);

    fireEvent.change(screen.getByLabelText('Display Name'), { target: { value: 'Actual Budget MCP' } });
    fireEvent.change(screen.getByLabelText('Transport Type'), { target: { value: 'http' } });
    fireEvent.change(screen.getByLabelText('Category'), { target: { value: 'finance, homelab, core' } });
    fireEvent.change(screen.getByLabelText('Connection URL'), { target: { value: 'http://budget:5006/sse' } });
    fireEvent.change(screen.getByLabelText('Static API Token / Secret (Fallback)'), { target: { value: 'my-static-token' } });

    const submitBtn = screen.getByRole('button', { name: /save server/i });
    await act(async () => {
      fireEvent.click(submitBtn);
    });

    expect(saveSpy).toHaveBeenCalledWith({
      displayName: 'Actual Budget MCP',
      type: 'http',
      categories: ['finance', 'homelab', 'core'],
      url: 'http://budget:5006/sse',
      secretProvider: 'None',
      secretItemKey: '',
      authShape: 'bearer',
      customHeaderName: '',
      apiKey: 'my-static-token',
      enabled: true,
      hidden: false
    });
  });
});
