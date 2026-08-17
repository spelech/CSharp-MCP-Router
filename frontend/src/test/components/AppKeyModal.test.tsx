import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import { AppKeyModal } from '../../components/clients/AppKeyModal';
import { useAppKeyStore } from '../../stores/useAppKeyStore';

describe('AppKeyModal component', () => {
  /**
   * @requirement UI-01
   * @category UI
   * @type PositiveFeature
   * @description Renders the dashboard and visualizes MCP server states
   */
  it('renders nothing when isCreateModalOpen is false', () => {
    useAppKeyStore.setState({ isCreateModalOpen: false, createdResult: null });
    const { container } = render(<AppKeyModal />);
    expect(container.firstChild).toBeNull();
  });

  /**

   * @requirement UI-01

   * @category UI

   * @type PositiveFeature

   * @description Renders the dashboard and visualizes MCP server states

   */

  it('renders form and handles scope serialization for Full Gateway Access (all)', async () => {
    const createSpy = vi.fn().mockResolvedValue(undefined);
    useAppKeyStore.setState({ isCreateModalOpen: true, createdResult: null, createAppKey: createSpy });
    render(<AppKeyModal />);

    expect(screen.getByText('Create New App Key')).toBeInTheDocument();
    fireEvent.change(screen.getByPlaceholderText('e.g. My Laptop CLI'), { target: { value: 'Cursor Workspace' } });

    const submitBtn = screen.getByRole('button', { name: /generate app key/i });
    await act(async () => {
      fireEvent.click(submitBtn);
    });

    expect(createSpy).toHaveBeenCalledWith({
      name: 'Cursor Workspace',
      scopes: ['all'],
      expiresInDays: undefined
    });
  });

  /**

   * @requirement UI-01

   * @category UI

   * @type PositiveFeature

   * @description Renders the dashboard and visualizes MCP server states

   */

  it('handles scope serialization for server scope', async () => {
    const createSpy = vi.fn().mockResolvedValue(undefined);
    useAppKeyStore.setState({ isCreateModalOpen: true, createdResult: null, createAppKey: createSpy });
    render(<AppKeyModal />);

    fireEvent.change(screen.getByPlaceholderText('e.g. My Laptop CLI'), { target: { value: 'Notes Assistant' } });

    // Select Server scope
    const scopeSelect = screen.getByText('Scope / Access Level').closest('.form-group')!.querySelector('select')!;
    fireEvent.change(scopeSelect, { target: { value: 'server' } });

    // Enter server target name
    const targetInput = screen.getByPlaceholderText('e.g. ha, docker');
    fireEvent.change(targetInput, { target: { value: 'notes-rag' } });

    const submitBtn = screen.getByRole('button', { name: /generate app key/i });
    await act(async () => {
      fireEvent.click(submitBtn);
    });

    expect(createSpy).toHaveBeenCalledWith({
      name: 'Notes Assistant',
      scopes: ['server:notes-rag'],
      expiresInDays: undefined
    });
  });

  /**

   * @requirement UI-01

   * @category UI

   * @type PositiveFeature

   * @description Renders the dashboard and visualizes MCP server states

   */

  it('handles scope serialization for category scope and expiration days', async () => {
    const createSpy = vi.fn().mockResolvedValue(undefined);
    useAppKeyStore.setState({ isCreateModalOpen: true, createdResult: null, createAppKey: createSpy });
    render(<AppKeyModal />);

    fireEvent.change(screen.getByPlaceholderText('e.g. My Laptop CLI'), { target: { value: 'Media Tools' } });

    // Select Category scope
    const scopeSelect = screen.getByText('Scope / Access Level').closest('.form-group')!.querySelector('select')!;
    fireEvent.change(scopeSelect, { target: { value: 'category' } });

    const targetInput = screen.getByPlaceholderText('e.g. smarthome, media');
    fireEvent.change(targetInput, { target: { value: 'media' } });

    // Select 90 days expiration
    const expSelect = screen.getByText('Expiration').closest('.form-group')!.querySelector('select')!;
    fireEvent.change(expSelect, { target: { value: '90' } });

    const submitBtn = screen.getByRole('button', { name: /generate app key/i });
    await act(async () => {
      fireEvent.click(submitBtn);
    });

    expect(createSpy).toHaveBeenCalledWith({
      name: 'Media Tools',
      scopes: ['category:media'],
      expiresInDays: 90
    });
  });

  /**

   * @requirement UI-01

   * @category UI

   * @type PositiveFeature

   * @description Renders the dashboard and visualizes MCP server states

   */

  it('disables submit button when quota limit is reached', () => {
    useAppKeyStore.setState({
      isCreateModalOpen: true,
      createdResult: null,
      limits: {
        globalMax: 50,
        userMax: 10,
        totalActiveKeys: 50,
        userActiveKeys: 10,
        isLimitReached: true
      }
    });

    render(<AppKeyModal />);

    const submitBtn = screen.getByRole('button', { name: /generate app key/i });
    expect(submitBtn).toBeDisabled();
  });

  /**

   * @requirement UI-01

   * @category UI

   * @type PositiveFeature

   * @description Renders the dashboard and visualizes MCP server states

   */

  it('displays one-time secret result and copies plaintext key to clipboard', async () => {
    const closeSpy = vi.fn();
    useAppKeyStore.setState({
      isCreateModalOpen: true,
      createdResult: {
        id: 'key-99',
        name: 'CLI Agent',
        username: 'admin',
        keyPrefix: 'mcp_live_9999',
        plaintextKey: 'mcp_live_9999_secret_token_123456789',
        scopes: ['all'],
        createdAt: '2026-08-14T00:00:00Z'
      },
      closeModal: closeSpy
    });

    render(<AppKeyModal />);

    expect(screen.getByText('App Key Created!')).toBeInTheDocument();
    expect(screen.getByText('mcp_live_9999_secret_token_123456789')).toBeInTheDocument();
    expect(screen.getByText(/Ready-to-Use mcp_config\.json Snippet:/i)).toBeInTheDocument();

    // Copy to clipboard
    const copyBtn = document.querySelector('.fa-copy')?.closest('button');
    expect(copyBtn).toBeInTheDocument();

    await act(async () => {
      fireEvent.click(copyBtn!);
    });

    expect(navigator.clipboard.writeText).toHaveBeenCalledWith('mcp_live_9999_secret_token_123456789');

    // Done button closes modal
    const doneBtn = screen.getByRole('button', { name: /done/i });
    fireEvent.click(doneBtn);
    expect(closeSpy).toHaveBeenCalled();
  });
});
